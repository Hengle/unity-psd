﻿using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using PSDUnity;
namespace PSDUnity.UGUI
{
    public class GridLayerImport : ILayerImport
    {
        PSDImportCtrl ctrl;
        public GridLayerImport(PSDImportCtrl ctrl)
        {
            this.ctrl = ctrl;
        }
        public UGUINode DrawLayer(GroupNode layer, UGUINode parent)
        {
            UGUINode node = PSDImporter.InstantiateItem(GroupType.GRID, layer.displayName, parent);
            node.anchoType = AnchoType.Up | AnchoType.Left;
            GridLayoutGroup gridLayoutGroup = node.InitComponent<GridLayoutGroup>();
            PSDImporter.SetRectTransform(layer, gridLayoutGroup.GetComponent<RectTransform>());

            gridLayoutGroup.padding = new RectOffset(1,1,1,1);
            gridLayoutGroup.cellSize = new Vector2(layer.rect.width, layer.rect.height);

            switch (layer.direction)
            {
                case Direction.Horizontal:
                    gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                    break;
                case Direction.Vertical:
                    gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                    break;
                default:
                    gridLayoutGroup.constraint = GridLayoutGroup.Constraint.Flexible;
                    break;
            }
            gridLayoutGroup.constraintCount = layer.constraintCount;

            ctrl.DrawImages(layer.images.ToArray(), node);
            if (layer.children != null)
                ctrl.DrawLayers(layer.children.ConvertAll(x=>x as GroupNode).ToArray() as GroupNode[], node);
            return node;
        }
    }
}
