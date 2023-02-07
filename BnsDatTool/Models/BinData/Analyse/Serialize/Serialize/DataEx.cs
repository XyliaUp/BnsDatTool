using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

using Xylia.bns.Modules.DataFormat.Analyse.Input;
using Xylia.bns.Modules.DataFormat.Analyse.Value;
using Xylia.bns.Modules.DataFormat.Bin;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT;
using Xylia.bns.Modules.DataFormat.Bin.Entity.BDAT.Interface;
using Xylia.bns.Util;
using Xylia.bns.Util.Sort;
using Xylia.Extension;
using Xylia.Files.XmlEx;

namespace Xylia.bns.Modules.DataFormat.Analyse.Serialize
{
    public static class DataEx
    {
        public static List<KeyValuePair<string, IObject>> GetAliasInfo(this BinData BinHandle, string Signal)
        {
            var Result = new List<KeyValuePair<string, IObject>>();

            //获取目标表名
            var Target = BinHandle.GetList(Signal);
            if (!Target.HasValue || !BinHandle._content.Lists.ContainsKey(Target.Value)) throw new Exception($"未包含处理目标 => { Signal }");

            var List = BinHandle._content.Lists[Target.Value];
            if (List is BDAT_ARCHIVE Archive) foreach (var t in Archive.Tables) Result.Add(new(t.Alias, t));

            return Result;
        }

        /// <summary>
        /// 部分序列化（只重构数据，不重构表unk信息）
        /// </summary>
        /// <param name="BinHandle"></param>
        /// <param name="ListInfo"></param>
        /// <param name="ParentMsg"></param>
        public static void HandlePart(this BinData BinHandle, SeriListTableInfo ListInfo, MessageHandle ParentMsg)
        {
            #region 初始化
            int TotalInput = ListInfo.Inputs.Count;

            //记录临时id，解决无id问题
            int Current = 0;
            bool HasType = false;

            //获取目标表名
            var Target = BinHandle.GetList(ListInfo.TableInfo.Type);
            if (!Target.HasValue || !BinHandle._content.Lists.ContainsKey(Target.Value)) throw new Exception($"未包含处理目标 => { ListInfo.TableInfo.Type }");

            var List = BinHandle._content.Lists[Target.Value];

            //定义增长活动
            MessageHandle SeriMsg = new(ListInfo.Inputs.Count, ParentMsg);
            #endregion

            #region	ReadIds
            HashSet<int> ReadIds = new();
            if (!ListInfo.TableInfo.Rules.Contains(Enums.ListRule.Complete))
                ListInfo.Inputs.ForEach(input => ReadIds.Add(input.BasicInfo.MainId.Main));
            #endregion


            //用于保存对象
            ObjectCollection Objects = new();

            #region Compress 序列化方法
            if (List is BDAT_ARCHIVE Archive)
            {
                //处理索引偏移
                Parallel.ForEach(ListInfo.Inputs, input =>
                {
                    var Lookups = input.Lookups;
                    if (Lookups.Count != 0)
                    {
                        //累积偏移
                        int AccumulativeOffset = 0;
                        foreach (var l in Lookups)
                        {
                            l.StringOffset = AccumulativeOffset;
                            AccumulativeOffset += l.Size;
                        }
                    }
                });

                #region 处理数据
                //直接重构
                if (ListInfo.TableInfo.Rules.Contains(Enums.ListRule.Complete))
                {
                    //如果定义了序列化范围
                    if (ListInfo.BeginTarget != 0)
                    {
                        foreach (BDAT_TABLE btable in Archive.Tables.Where(t => t.FID < ListInfo.BeginTarget)) Objects.Add(btable);
                    }

                    AddNew(ListInfo.Inputs, ListInfo.TableInfo.Rules, true, SeriMsg, Objects);
                }

                //替换原数据
                else
                {
                    #region 处理替换数据
                    foreach (BDAT_TABLE btable in Archive.Tables)
                    {
                        //查询失败，返回
                        if (!ReadIds.Contains(btable.Field.ID))
                        {
                            Objects.Add(btable);
                            continue;
                        }

                        //获得当前的分支id
                        int VariationId = btable.Field.VariationId.Main;

                        //如果包含主键，则查询重复次数
                        //找到对应的阶段
                        var oStruct = ListInfo.Inputs.Find(info => info.BasicInfo.MainId.Main == btable.Field.ID && info.BasicInfo.Level.Main == (VariationId == 0 ? 1 : VariationId));
                        if (oStruct != null)
                        {
                            #region 初始化
                            //移除
                            if (!ListInfo.Inputs.Remove(oStruct)) LogWriter.WriteLine("移除失败");

                            btable.Field.XmlNodeType = oStruct.BasicInfo.XmlNodeType;
                            btable.Field.SubclassType = oStruct.BasicInfo.SubclassType;
                            #endregion

                            #region 数据处理
                            var data = ListInfo.TableInfo.Rules.Contains(Enums.ListRule.cover) ? new byte[btable.Field.Data.Length] : btable.Field.Data;
                            oStruct.Cells.ForEach(c => c.StructureData(ref data, true));  //开始生成
                            #endregion

                            #region 最后处理
                            btable.Field.VariationId.Main = VariationId;
                            btable.Field.Data = data;

                            //替换明文数据
                            btable.Lookup.TextList = new StringList(oStruct.Lookups);

                            Objects.Add(btable);
                            #endregion
                        }
                    }
                    #endregion

                    //处理新增部分数据
                    AddNew(ListInfo.Inputs, ListInfo.TableInfo.Rules, true, SeriMsg, Objects);
                }
                #endregion

                //排序与转换后拆分
                Archive.SubArcsList = new BDAT_SUBARCHIVE(Objects.Select(f => (BDAT_TABLE)f)).Split();
            }
            #endregion

            #region Loose 序列化方法
            else if (List is BDAT_LOOSE Loose)
            {
                #region 初始化
                //处理公共索引偏移
                int AccumulativeOffset = 0;
                var GlobalLookup = new List<string>();
                foreach (var inputs in ListInfo.Inputs)
                {
                    var Lookups = inputs.Lookups;
                    if (Lookups.Count != 0)
                    {
                        GlobalLookup.AddRange(Lookups.Select(l => l.Val));
                        foreach (var l in Lookups)
                        {
                            l.StringOffset = AccumulativeOffset;
                            AccumulativeOffset += l.Size;
                        }
                    }
                }

                //存储文本
                Loose.Lookup.Data = BDAT_LOOKUPTABLE.WordToData(GlobalLookup);
                GlobalLookup = null;
                #endregion

                #region 处理数据
                //直接重构
                if (ListInfo.TableInfo.Rules.Contains(Enums.ListRule.Complete))
                {
                    //如果定义了序列化范围
                    if (ListInfo.BeginTarget != 0)
                    {
                        foreach (var btable in Loose.Fields.Where(t => t.FID < ListInfo.BeginTarget))
                            Objects.Add(btable);
                    }

                    //处理新增
                    AddNew(ListInfo.Inputs, ListInfo.TableInfo.Rules, false, SeriMsg, Objects);

                    //添加一个Null的Field   
                    if (ListInfo.TableInfo.Rules.Contains(Enums.ListRule.extra)) Objects.Add(null);
                }

                //替换原数据
                else
                {
                    //Field部分
                    bool HasExtra = Loose.Fields.Length != 0 && Loose.Fields.Last() is null;
                    foreach (BDAT_FIELDTABLE f in Loose.Fields.Where(f => f != null))
                    {
                        #region 初始化
                        //查询失败，返回
                        if (!ReadIds.Contains(f.ID))
                        {
                            Objects.Add(f);
                            continue;
                        }

                        //如果包含主键，则查询重复次数
                        BnsId Variation = new(f.Data);
                        #endregion

                        #region 找到对应的阶段
                        var oStruct = ListInfo.Inputs.Find(Input => Input.BasicInfo.MainId.Main == f.ID && Input.BasicInfo.Level.Main == Variation.Main);
                        if (oStruct != null)
                        {
                            #region 初始化
                            if (!ListInfo.Inputs.Remove(oStruct)) LogWriter.WriteLine("移除失败");

                            f.XmlNodeType = oStruct.BasicInfo.XmlNodeType;   
                            f.SubclassType = oStruct.BasicInfo.SubclassType;  
                            #endregion

                            #region 存储数据
                            var data = ListInfo.TableInfo.Rules.Contains(Enums.ListRule.cover) ? new byte[f.Data.Length] : f.Data;
                            oStruct.Cells.ForEach(c => c.StructureData(ref data, false));  //开始生成

                            f.Data = data;
                            f.VariationId = Variation;
                            #endregion

                            Objects.Add(f);
                            data.Field_Modify(f.ID);   //日志
                        }
                        #endregion
                    };

                    //处理新增
                    AddNew(ListInfo.Inputs, ListInfo.TableInfo.Rules, false, SeriMsg, Objects);

                    //判断有无额外数据
                    if (HasExtra) Objects.Add(null);
                }
                #endregion

                #region 最后处理 & 资源清理
                Loose.Fields = Objects.Select(f => (BDAT_FIELDTABLE)f).ToArrayCustom(!ListInfo.TableInfo.Rules.Contains(Enums.ListRule.unsort));
                
                //重新生成编号信息
                if (ListInfo.TableInfo.Rules.Contains(Enums.ListRule.SortId))
                {
                    int CurIndex = (int)ListInfo.TableInfo.AutoStart;
                    foreach (var f in Loose.Fields.Where(f => f != null)) f.ID = f.TableIndex = CurIndex++;
                }
                #endregion
            }
            #endregion



            //创建别名表
            if (ListInfo.TableInfo.CreateAliasTable)
            {
                XmlInfo xi = new();
                foreach (var o in Objects)
                {
                    var Xe = (XmlElement)xi.AppendChild(xi.CreateElement("record"));
                    Xe.SetAttribute("alias", o.CacheAlias);
                    Xe.SetAttribute("id", o.FID);

                    if (o.FLevel != null) Xe.SetAttribute("level", o.FLevel.Main);    
                }

                try
                {
                    xi.Save(BNSFileHelper.GetAliasTableOutPath(ListInfo), true); 
                }
                catch (Exception ex)
                { 
                    Console.WriteLine($"[{ListInfo.TableInfo.Type}] " + ex.Message);
                }
            }

            //清理资源
            Objects.Clear();
        }


        /// <summary>
        /// 增加新对象
        /// </summary>
        /// <param name="Inputs"></param>
        /// <param name="Rules"></param>
        /// <param name="IsCompress"></param>
        /// <param name="SeriMsg"></param>
        /// <param name="lst"></param>
        /// <returns></returns>
        public static List<IObject> AddNew(IEnumerable<Input.Input> Inputs, List<Enums.ListRule> Rules, bool IsCompress, MessageHandle SeriMsg, List<IObject> lst = null)
        {
            bool HasType = false;
            var Result = new BlockingCollection<IObject>();

            Parallel.ForEach(Inputs, input =>
            {
                SeriMsg?.Update();

                #region 生成数据信息
                var MainId = input.BasicInfo.MainId;

                // 生成数据
                var data = input.StructureData(IsCompress);
                if (data.Length < 4) throw new Exception("数据长度异常 " + MainId);

                BnsId Variation = new(data);
                if (Variation.Main == 0 && Rules.Contains(Enums.ListRule.UseAutoVariation)) Variation.Main = input.BasicInfo.Level.Main;
                #endregion

                #region 创建数据实例
                IObject IObject = IsCompress ?
                     ObjectEx.newTable(MainId.Main, Variation, data, input.Lookups.Select(input => input.Val), input.BasicInfo.XmlNodeType, input.BasicInfo.SubclassType) :
                     ObjectEx.newField(MainId.Main, Variation, data, input.BasicInfo.XmlNodeType, input.BasicInfo.SubclassType);

                IObject.TableIndex = input.BasicInfo.TableIndex; //传递索引
                IObject.CacheAlias = input.BasicInfo.Alias;      //缓存别名

                Result.Add(IObject);
                #endregion
            });


            //返回结果
            if (lst is null) return Result.ToList();
            else if (lst is ObjectCollection objects) objects.AddRange(Result);
            else lst.AddRange(Result);

            return lst;
        }

        /// <summary>
        /// 构建数据
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="data"></param>
        /// <param name="ListIsCompress">指示表的压缩状态</param>
        public static void StructureData(this InputCell cell, ref byte[] data, bool ListIsCompress)
        {
            if (cell.Record is null) throw new ArgumentNullException("无效记录器");

            //排除非Data需要类型
           // else if (!cell.Record.ValueType.IsFieldType()) return;
            //else if (cell.Record.ValueType == VType.TLevel)
            //{
            //    data = data.BlockCopy(cell.InputVal, 0);
            //    return;
            //}
            else if (cell.Record.ValueType == VType.TString || cell.Record.ValueType == VType.TNative)
            {
                if (cell is not StringInputCell @scell) throw new Exception("类型非法");

                data.BlockCopy(BitConverter.GetBytes(@scell.StringOffset), cell.GetOffset());
                return;
            }

            //如果没有数值，返回不进行处理
            else if (cell.InputVal is null) return;
            else data.BlockCopy(cell.InputVal, cell.GetOffset());
        }
    }
}