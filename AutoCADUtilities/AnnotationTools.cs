﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Internal;

namespace DotNetARX
{
    public static class AnnotationTools
    {
        /// <summary>
        /// 添加注释比例对象
        /// </summary>
        /// <param name="db">图形数据库</param>
        /// <param name="scaleName">注释比例名称</param>
        /// <param name="paperUnits">图纸单位</param>
        /// <param name="drawingUnits">绘图单位</param>
        /// <returns></returns>
        public static AnnotationScale AddScale(this Database db, string scaleName,
            double paperUnits,double drawingUnits)
        {
            AnnotationScale scale = null;//申明一个注释比例对象
            //获取当前图形的对象比例管理器
            ObjectContextManager ocm = db.ObjectContextManager;
            //获取当前图形的注释比例列表，名为ACDB_ANNOTATIONSCALES
            ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
            //如果没有名为scaleName的注释比例
            if(!occ.HasContext(scaleName))
            {
                scale = new AnnotationScale();//新建一个注释比例
                scale.Name = scaleName;//设置注释比例名
                scale.PaperUnits = paperUnits;//设置图纸单位
                scale.DrawingUnits = drawingUnits;//设置绘图单位
                occ.AddContext(scale);//将新建注释比例添加至注释比例集
            }
            return scale;//返回新建的注释比例
        }
        /// <summary>
        /// 为实体添加指定的注释比例
        /// </summary>
        /// <param name="entId">实体的Id</param>
        /// <param name="scaleNames">注释比例名列表</param>
        public static void AttachScale(this ObjectId entId, params string[] scaleNames)
        {
            Database db = entId.Database;
            DBObject obj = entId.GetObject(OpenMode.ForRead);//获取entId指示的实体对象
            if (obj.Annotative != AnnotativeStates.NotApplicable) //如果实体支持注释缩放
            {
                //如果实体为块参照，则通过其所属的块表来进行操作
                if (obj is BlockReference)
                {
                    BlockReference br = obj as BlockReference;
                    //打开对应的块表记录
                    BlockTableRecord btr = br.BlockTableRecord.GetObject(OpenMode.ForWrite) as BlockTableRecord;
                    btr.Annotative = AnnotativeStates.True;//设置块表记录为可注释缩放
                }
                //其他可注释缩放的实体，使其可注释缩放
                else if (obj.Annotative == AnnotativeStates.False)
                    obj.Annotative = AnnotativeStates.True;
                obj.UpgradeOpen();//切换实体为写状态以添加注释比例
                //获取当前图形的对象比例管理器
                ObjectContextManager ocm = db.ObjectContextManager;
                //获取当前图形的注释比例列表，名为ACDB_ANNOTATIONSCALES
                ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                foreach (string scaleName in scaleNames)//遍历需要设置的注释比例
                {
                    //获取名为scaleName的注释比例
                    ObjectContext scale = occ.GetContext(scaleName);
                    //若不存在scaleName的注释比例，则结束本次循环
                    if (scaleName == null) continue;
                    //为实体添加名为scaleName的注释比例
                    ObjectContexts.AddContext(obj, scale);
                }
                obj.DowngradeOpen();
            }
        }
        /// <summary>
        /// 获取实体拥有的所有缩放比例
        /// </summary>
        /// <param name="entId">实体的Id</param>
        /// <returns>返回实体的缩放比例列表</returns>
        public static List<ObjectContext> GetAllScales(this ObjectId entId)
        {
            //声明一个列表对象，用于返回实体所拥有的所有的注释比例
            List<ObjectContext> scales = new List<ObjectContext>();
            DBObject obj = entId.GetObject(OpenMode.ForRead);
            if (obj.Annotative != AnnotativeStates.NotApplicable)
            {
                ObjectContextManager ocm = obj.Database.ObjectContextManager;
                ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                //遍历注释比例列表
                foreach (ObjectContext oc in occ)
                {
                    //如果实体拥有此注释比例
                    if (ObjectContexts.HasContext(obj, oc))
                    {
                        scales.Add(oc);//将此注释比例添加到返回列表中
                    }
                }
            }
            return scales;//返回实体所拥有的所有的注释比例
        }

        /// <summary>
        /// 删除实体的缩放比例
        /// </summary>
        /// <param name="entId">实体的Id</param>
        /// <param name="scaleNames">缩放比例名列表</param>
        public static void RemoveScale(this ObjectId entId, params string[] scaleNames)
        {
            //获取entId指示的实体对象
            DBObject obj = entId.GetObject(OpenMode.ForRead);
            //如果实体对象支持注释缩放
            if (obj.Annotative != AnnotativeStates.NotApplicable)
            {
                //获得对象所有的注释比例
                List<ObjectContext> scales = entId.GetAllScales();
                obj.UpgradeOpen();//切换实体为写的状态
                //获取当前图形的对象比例管理器
                ObjectContextManager ocm = obj.Database.ObjectContextManager;
                //获取当前图形的注释比例列表，名为ACDB_ANNOTATIONSCALES
                ObjectContextCollection occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
                //遍历需要设置的注释比例
                foreach (string scaleName in scaleNames)
                {
                    //获取名为scaleName的注释比例
                    ObjectContext scale = occ.GetContext(scaleName);
                    //若不存在scaleName的注释比例，则结束本次循环
                    if (scale == null) continue;
                    //删除名为scaleName的注释比例
                    ObjectContexts.RemoveContext(obj, scale);
                }
                obj.DowngradeOpen();//为了安全将实体切换为读的状态
            }
        }
    }
}
