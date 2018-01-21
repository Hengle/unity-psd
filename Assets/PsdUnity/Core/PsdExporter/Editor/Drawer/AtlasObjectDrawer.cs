﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEditor;
using Rotorz.ReorderableList;
using System;
using Ntreev.Library.Psd;
using PSDUnity.Data;
using PSDUnity.Import;

namespace PSDUnity.Exprot
{
    [CustomEditor(typeof(AtlasObject))]
    public class AtlasObjectDrawer : Editor
    {
        SerializedProperty scriptProp;
        SerializedProperty psdFileProp;
        SerializedProperty groupsProp;
        SerializedProperty ruleObjProp;
        SerializedProperty settingObjProp;
        AtlasObject atlasObj;
        private const string Prefs_LastPsdsDir = "lastPsdFileDir";
        private void OnEnable()
        {
            atlasObj = target as AtlasObject;
            scriptProp = serializedObject.FindProperty("m_Script");
            psdFileProp = serializedObject.FindProperty("psdFile");
            groupsProp = serializedObject.FindProperty("groups");
            ruleObjProp = serializedObject.FindProperty("ruleObj");
            settingObjProp = serializedObject.FindProperty("settingObj");
            AutoChargeRule();
        }

        private void AutoChargeRule()
        {
            if (atlasObj.ruleObj == null)
            {
                var path = AssetDatabase.GUIDToAssetPath("f7d3181f5b8957245adfabda058c8541");
                atlasObj.ruleObj = AssetDatabase.LoadAssetAtPath<RuleObject>(path);
            }

            if (atlasObj.settingObj == null)
            {
                var path = AssetDatabase.GUIDToAssetPath("79102a4c6ecda994b9437a6c701177a2");
                atlasObj.settingObj = AssetDatabase.LoadAssetAtPath<SettingObject>(path);
            }
        }
        protected override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(atlasObj, typeof(AtlasObject), false);
            EditorGUILayout.PropertyField(scriptProp);
            EditorGUI.EndDisabledGroup();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPathOption();
            DrawPictureOption();
            DrawGroupNode();
            DrawUICreateOption();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawUICreateOption()
        {
            using (var hor = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(ruleObjProp, true);
                if (GUILayout.Button("创建规则"))
                {
                    var obj = RuleObject.CreateInstance<RuleObject>();
                    ProjectWindowUtil.CreateAsset(obj, "Rule.asset");
                    ruleObjProp.objectReferenceValue = obj;
                }
                if (GUILayout.Button("导出到UI"))
                {
                    var atlasObj = (AtlasObject)target;
                    var canvasObj = Array.Find(Selection.objects,x=>x is GameObject && (x as GameObject).GetComponent<Canvas>()!= null);
                    PSDImporter.InitEnviroment(atlasObj.ruleObj, atlasObj.settingObj.defultUISize, canvasObj == null ? null: (canvasObj as GameObject).GetComponent<Canvas>());
                    PSDImporter.StartBuild(atlasObj.groups.ToArray());
                    AssetDatabase.Refresh();
                }
            }

        }
        private void DrawGroupNode()
        {
            ReorderableListGUI.Title("信息列表");
            ReorderableListGUI.ListField(groupsProp);
        }

        private void SwitchLayerToTexture()
        {
            if (!string.IsNullOrEmpty(atlasObj.psdFile))
            {
                var psd = PsdDocument.Create(atlasObj.psdFile);
                if (psd != null)
                {
                    PsdExportUtility.InitPsdExportEnvrioment(atlasObj, new Vector2(psd.Width, psd.Height));
                    atlasObj.groups.Clear();
                    var groupDatas = PsdExportUtility.CreatePictures(psd.Childs, new Vector2(psd.Width, psd.Height), atlasObj.settingObj.defultUISize, atlasObj.settingObj.forceSprite);
                    if (groupDatas != null)
                    {
                        foreach (var groupData in groupDatas)
                        {
                            PsdExportUtility.ChargeTextures(atlasObj, groupData);
                            atlasObj.groups.Add(groupData);
                        }
                    }

                    EditorUtility.SetDirty(atlasObj);
                }
            }
        }


        private void DrawPathOption()
        {
            using (var hor = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel("文档路径:", GUILayout.Width(60));
                if (GUILayout.Button(new GUIContent(psdFileProp.stringValue, "点击此处选择文件夹！"), EditorStyles.textField))
                {
                    var oldDir = PlayerPrefs.GetString(Prefs_LastPsdsDir);
                    if (string.IsNullOrEmpty(oldDir))
                    {
                        oldDir = Application.dataPath;
                    }

                    var dir = string.IsNullOrEmpty(psdFileProp.stringValue) ? oldDir : System.IO.Path.GetDirectoryName(psdFileProp.stringValue);
                    var path = EditorUtility.OpenFilePanel("选择一个pdf文件", dir, "psd");
                    if (!string.IsNullOrEmpty(path))
                    {
                        psdFileProp.stringValue = path;
                        PlayerPrefs.SetString(Prefs_LastPsdsDir, System.IO.Path.GetDirectoryName(path));
                    }
                }
            }
        }
        private void DrawPictureOption()
        {
            using (var hor = new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(settingObjProp, true);
                if (GUILayout.Button("创建规则"))
                {
                    var obj = RuleObject.CreateInstance<SettingObject>();
                    ProjectWindowUtil.CreateAsset(obj, "Setting.asset");
                    settingObjProp.objectReferenceValue = obj;
                }
                if (GUILayout.Button("读取层级"))
                {
                    SwitchLayerToTexture();
                }
            }


        }

    }

}