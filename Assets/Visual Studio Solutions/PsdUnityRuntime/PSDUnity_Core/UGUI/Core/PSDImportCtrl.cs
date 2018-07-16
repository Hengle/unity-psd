﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using PSDUnity;

namespace PSDUnity.UGUI
{
    public class PSDImportCtrl
    {
        private Dictionary<ImgType, ImageImport> imgImporterDic;
        private Dictionary<string, LayerImport> layerImporterDic;
        public Canvas canvas { get; private set; }
        public  RuleObject rule { get; private set; }
        public  UGUINode uinode { get; private set; }
        public Vector2 canvasSize { get; private set; }

        public PSDImportCtrl(Canvas canvas,RuleObject rule,Vector2 canvasSize)
        {
            this.canvas = canvas;
            this.rule = rule;
            this.canvas = canvas;
            this.canvasSize = canvasSize;
            uinode = new UGUINode(canvas.transform, null);
            InitDrawers();
        }

        private void InitDrawers()
        {
            imgImporterDic = new Dictionary<ImgType, ImageImport>();
            imgImporterDic.Add(ImgType.Texture, ScriptableObject.CreateInstance<TextureImport>());
            imgImporterDic.Add(ImgType.Image, ScriptableObject.CreateInstance<SpriteImport>());
            imgImporterDic.Add(ImgType.AtlasImage, ScriptableObject.CreateInstance<SpriteImport>());
            imgImporterDic.Add(ImgType.Color, ScriptableObject.CreateInstance<SpriteImport>());
            imgImporterDic.Add(ImgType.Label, ScriptableObject.CreateInstance<TextImport>());
     

            layerImporterDic = new Dictionary<string, LayerImport>();
            layerImporterDic.Add(PSDUnityConst.emptySuffix, ScriptableObject.CreateInstance<PanelLayerImport>());
            foreach (var item in rule.layerImports)
            {
                layerImporterDic.Add(item.Suffix, item);
            }
        }

        public void Import(Data.GroupNode rootNode)
        {
            InitBaseSize(uinode, canvasSize);
            DrawLayer(rootNode, uinode);//直接绘制所有层级
            BeginSetUIParents(uinode);//设置层级之前的父子关系
            BeginSetAnchers(uinode);//设置层级的锚点
            BeginReprocess(uinode);//后处理
            if (rule.scaleWithCanvas){
                BeginScaleWithCanvas(uinode, canvasSize);//尺寸缩放
            }
        }

        private void InitBaseSize(UGUINode uinode,Vector2 uiSize)
        {
            var rect = uinode.InitComponent<RectTransform>();
            rect.sizeDelta = uiSize;
        }

        private void BeginScaleWithCanvas(UGUINode uinode,Vector2 uiSize)
        {
            foreach (var item in uinode.childs)
            {
                var child = item.InitComponent<RectTransform>();
                child.anchorMin = Vector2.zero;
                child.anchorMax = Vector2.one;
                child.anchoredPosition = Vector2.zero;
            }
        }

        public UGUINode DrawLayer(Data.GroupNode layer, UGUINode parent)
        {
            UGUINode node = layerImporterDic[layer.suffix].DrawLayer(layer, parent);
            return node;
        }

        public UGUINode[] DrawLayers(Data.GroupNode[] layers, UGUINode parent)
        {
            UGUINode[] nodes = new UGUINode[layers.Length];
            if (layers != null)
            {
                for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
                {
                    nodes[layerIndex] = DrawLayer(layers[layerIndex], parent);
                }
            }
            return nodes;
        }

        public UGUINode[] DrawImages(Data.ImgNode[] images, UGUINode parent)
        {
            UGUINode[] nodes = new UGUINode[images.Length];
            if (images != null)
            {
                for (int layerIndex = 0; layerIndex < images.Length; layerIndex++)
                {
                    nodes[layerIndex] = DrawImage(images[layerIndex], parent);
                }
            }
            return nodes;
        }

        public UGUINode DrawImage(Data.ImgNode image, UGUINode parent)
        {
            UGUINode node = imgImporterDic[image.type].DrawImage(image,parent);
            if(node == null)
            {
                Debug.Log(image.type);
                Debug.Log(image);
                Debug.Log(parent);
            }
            return node;
        }
        
        public void BeginSetUIParents(UGUINode node)
        {
            foreach (var item in node.childs)
            {
                item.transform.SetParent(node.transform);
                BeginSetUIParents(item);
            }
        }

        public void BeginSetAnchers(UGUINode node)
        {
            foreach (var item in node.childs)
            {
                BeginSetAnchers(item);
                PSDImporterUtility.SetAnchorByNode(item);
            }
        }

        public void BeginReprocess(UGUINode node)
        {
            foreach (var item in node.childs)
            {
                BeginReprocess(item);
            }
            node.InversionReprocess();
        }
    }
}