using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

//------------------------------------------------------------------------------
// class definition
//------------------------------------------------------------------------------
public class CommonPSDImporter : Editor
{
    //--------------------------------------------------------------------------
    // static private properties
    //--------------------------------------------------------------------------
    private static string baseFilename;
    private static string baseDirectory;

    [MenuItem("Assets/Create/Import PSD ...")]
    static public void ImportHogSceneMenuItem()
    {
        string inputFile = EditorUtility.OpenFilePanel("Choose PSDUI File to Import", Application.dataPath, "xml");
        if ((inputFile != null) && (inputFile != "") && (inputFile.StartsWith(Application.dataPath)))
        {
            ImportPSDUI(inputFile);
        }
    }

    static private void ImportPSDUI(string assetPath)
    {
        // before we do anything else, try to deserialize the input file and be sure it's actually the right kind of file
        PSDUI psdUI = (PSDUI)DeserializeXml(assetPath, typeof(PSDUI));

        Debug.Log(psdUI.psdSize.width + "=====psdSize======" + psdUI.psdSize.height);

        if (psdUI == null)
        {
            Debug.Log("The file " + assetPath + " wasn't able to generate a PSDUI.");
            return;
        }

        // next, we're going to be creating scenes, allow the user to save if they want
        // see if user wants to save current scene, bail if they don't
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()/*EditorApplication.SaveCurrentSceneIfUserWantsTo () */== false)
        {
            return;
        }

        // cache some useful variables
        baseFilename = Path.GetFileNameWithoutExtension(assetPath);
        baseDirectory = "Assets/" + Path.GetDirectoryName(assetPath.Remove(0, Application.dataPath.Length + 1)) + "/";

        Debug.Log("baseFilename " + baseFilename);
        Debug.Log("baseDirectory " + baseDirectory);

        // if the scene already exists, delete it
        string scenePath = baseDirectory + baseFilename + " Scene.unity";
        if (File.Exists(scenePath) == true)
        {
            File.Delete(scenePath);
            AssetDatabase.Refresh();
        }
        // now create a new scene
        EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects);//  EditorApplication.NewScene ();

        for (int layerIndex = 0; layerIndex < psdUI.layers.Length; layerIndex++)
        {
            ImportLayer(psdUI.layers[layerIndex], baseDirectory);
        }

        Canvas temp = Resources.Load(PSDImporterConst.PREFAB_PATH_CANVAS, typeof(Canvas)) as Canvas;
        Canvas canvas = GameObject.Instantiate(temp) as Canvas;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(psdUI.psdSize.width, psdUI.psdSize.height);

        GameObject obj = CreateEmptyParent(baseFilename);
        obj.transform.SetParent(canvas.transform, false);

        for (int layerIndex = 0; layerIndex < psdUI.layers.Length; layerIndex++)
        {
            DrawLayer(psdUI.layers[layerIndex], obj);
            Debug.Log("Draw1" + psdUI.layers[layerIndex].name);
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        AssetDatabase.Refresh();
        //EditorSceneManager.SaveScene()
        //EditorApplication.SaveScene(scenePath);

        if (baseFilename.Contains("Common"))
        {
            for (int layerIndex = 0; layerIndex < psdUI.layers.Length; layerIndex++)
            {
                MoveAsset(psdUI.layers[layerIndex], baseDirectory);
            }

            AssetDatabase.Refresh();
        }
    }

    static private GameObject CreateEmptyParent(string parentName)
    {
        GameObject pfb = Resources.Load<GameObject>(PSDImporterConst.PREFAB_PATH_EMPTY);
        GameObject go = Instantiate<GameObject>(pfb);
        go.name = parentName;
        return go;
    }

    //--------------------------------------------------------------------------
    // private methods
    //-------------------------------------------------------------------------

    static private void ImportLayer(PSDUI.Layer layer, string baseDirectory)
    {
        if (layer.images != null)
        {
            for (int imageIndex = 0; imageIndex < layer.images.Length; imageIndex++)
            {
                // we need to fixup all images that were exported from PS
                PSDUI.Image image = layer.images[imageIndex];

                if (image.imageSource == PSDUI.ImageSource.Custom)
                {
                    string texturePathName = baseDirectory + layer.images[imageIndex].name + PSDImporterConst.PNG_SUFFIX;

                    Debug.Log(texturePathName);
                    // modify the importer settings
                    TextureImporter textureImporter = AssetImporter.GetAtPath(texturePathName) as TextureImporter;
                    textureImporter.textureType = TextureImporterType.Sprite;
                    textureImporter.spriteImportMode = SpriteImportMode.Single;
                    textureImporter.spritePackingTag = baseFilename;
                    textureImporter.maxTextureSize = 2048;

                    if (baseFilename.Contains("Common") && layer.name == PSDImporterConst.NINE_SLICE)  //If Psd's name contains "Common", then it's Common type;
                    {
                        textureImporter.spriteBorder = new Vector4(3, 3, 3, 3);   // Set Default Slice type Image's border to Vector4 (3, 3, 3, 3)
                    }

                    AssetDatabase.WriteImportSettingsIfDirty(texturePathName);
                    AssetDatabase.ImportAsset(texturePathName);
                }
            }
        }

        if (layer.layers != null)
        {
            for (int layerIndex = 0; layerIndex < layer.layers.Length; layerIndex++)
            {
                ImportLayer(layer.layers[layerIndex], baseDirectory);
            }
        }
    }

    //------------------------------------------------------------------
    //when it's a common psd, then move the asset to special folder
    //------------------------------------------------------------------
    static private void MoveAsset(PSDUI.Layer layer, string baseDirectory)
    {
        if (layer.images != null)
        {
            string newPath = baseDirectory;
            if (layer.name == PSDImporterConst.IMAGE)
            {
                newPath = baseDirectory + PSDImporterConst.IMAGE + "/";
                System.IO.Directory.CreateDirectory(newPath);
            }
            else
            if (layer.name == PSDImporterConst.NINE_SLICE)
            {
                newPath = baseDirectory + PSDImporterConst.NINE_SLICE + "/";
                System.IO.Directory.CreateDirectory(newPath);
            }

            Debug.Log("creating new folder : " + newPath);

            AssetDatabase.Refresh();

            for (int imageIndex = 0; imageIndex < layer.images.Length; imageIndex++)
            {
                // we need to fixup all images that were exported from PS
                PSDUI.Image image = layer.images[imageIndex];

                if (image.imageSource == PSDUI.ImageSource.Custom)
                {
                    string texturePathName = baseDirectory + layer.images[imageIndex].name + PSDImporterConst.PNG_SUFFIX;
                    string targetPathName = newPath + layer.images[imageIndex].name + PSDImporterConst.PNG_SUFFIX;

                    Debug.Log(texturePathName);
                    Debug.Log(targetPathName);

                    AssetDatabase.MoveAsset(texturePathName, targetPathName);
                }
            }
        }

        if (layer.layers != null)
        {
            for (int layerIndex = 0; layerIndex < layer.layers.Length; layerIndex++)
            {
                MoveAsset(layer.layers[layerIndex], baseDirectory);
            }
        }
    }
    static private void DrawChildLayer(PSDUI.Layer layer, GameObject parent)
    {
        if (layer.layers != null)
        {
            for (int layerIndex = 0; layerIndex < layer.layers.Length; layerIndex++)
            {
                DrawLayer(layer.layers[layerIndex], parent);
                Debug.Log("Draw2" + layer.layers[layerIndex].name);
            }
        }
    }
    static private void DrawNormalLayer(PSDUI.Layer layer, GameObject parent)
    {
        GameObject obj = CreateEmptyParent(layer.name);
        obj.transform.SetParent(parent.transform, false); //parent.transform;

        if (layer.images != null)
        {
            for (int imageIndex = 0; imageIndex < layer.images.Length; imageIndex++)
            {
                PSDUI.Image image = layer.images[imageIndex];

                if (image.imageSource == PSDUI.ImageSource.Custom)
                {
                    Image pic = Resources.Load(PSDImporterConst.PREFAB_PATH_IMAGE, typeof(Image)) as Image;

                    string assetPath = baseDirectory + image.name + PSDImporterConst.PNG_SUFFIX;
                    Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;

                    if (sprite == null)
                    {
                        Debug.Log("loading asset at path: " + baseDirectory + image.name);
                    }

                    pic.sprite = sprite;

                    Image myImage = GameObject.Instantiate(pic) as Image;
                    myImage.name = image.name;
                    myImage.transform.SetParent(obj.transform, false);//.parent = obj.transform;

                    RectTransform rectTransform = myImage.GetComponent<RectTransform>();
                    rectTransform.sizeDelta = new Vector2(image.size.width, image.size.height);
                    rectTransform.anchoredPosition = new Vector2(image.position.x, image.position.y);
                }
                else
                if (image.imageSource == PSDUI.ImageSource.Common)
                {
                    if (image.imageType == PSDUI.ImageType.Label)
                    {
                        Text text = Resources.Load(PSDImporterConst.PREFAB_PATH_TEXT, typeof(Text)) as Text;

                        Text myText = GameObject.Instantiate(text) as Text;

                        //                        myText.color = image.arguments[0];
                        //                        myText.font = image.arguments[1];
                        Debug.Log("Label Color : " + image.arguments[0]);
                        Debug.Log("fontSize : " + image.arguments[2]);

                        Color color;
                        if (UnityEngine.ColorUtility.TryParseHtmlString(("#" + image.arguments[0]), out color))
                        {
                            myText.color = color;
                        }

                        int size;
                        if (int.TryParse(image.arguments[2], out size))
                        {
                            myText.fontSize = size;
                        }

                        myText.text = image.arguments[3];
                        myText.transform.SetParent(obj.transform, false);//.parent = obj.transform;

                        RectTransform rectTransform = myText.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(image.size.width, image.size.height);
                        rectTransform.anchoredPosition = new Vector2(image.position.x, image.position.y);
                    }
                    else if (image.imageType == PSDUI.ImageType.Texture)
                    {
                        Image pic = Resources.Load(PSDImporterConst.PREFAB_PATH_IMAGE, typeof(Image)) as Image;
                        pic.sprite = null;
                        Image myImage = GameObject.Instantiate(pic) as Image;
                        myImage.name = image.name;
                        myImage.transform.SetParent(obj.transform, false);//.parent = obj.transform;

                        RectTransform rectTransform = myImage.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(image.size.width, image.size.height);
                        rectTransform.anchoredPosition = new Vector2(image.position.x, image.position.y);
                    }
                    else
                    {
                        Image pic = Resources.Load(PSDImporterConst.PREFAB_PATH_IMAGE, typeof(Image)) as Image;

                        string commonImagePath = PSDImporterConst.COMMON_BASE_FOLDER + image.name.Replace(".", "/") + PSDImporterConst.PNG_SUFFIX;
                        Debug.Log("==  CommonImagePath  ====" + commonImagePath);
                        Sprite sprite = AssetDatabase.LoadAssetAtPath(commonImagePath, typeof(Sprite)) as Sprite;
                        pic.sprite = sprite;

                        Image myImage = GameObject.Instantiate(pic) as Image;
                        myImage.name = image.name;
                        myImage.transform.SetParent(obj.transform, false);//.parent = obj.transform;

                        if (image.imageType == PSDUI.ImageType.SliceImage)
                        {
                            myImage.type = Image.Type.Sliced;
                        }

                        RectTransform rectTransform = myImage.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(image.size.width, image.size.height);
                        rectTransform.anchoredPosition = new Vector2(image.position.x, image.position.y);
                    }
                }
            }
        }

        DrawChildLayer(layer, obj);
    }


    static private void DrawButton(PSDUI.Layer layer, GameObject parent)
    {
        Button temp = Resources.Load(PSDImporterConst.PREFAB_PATH_BUTTON, typeof(Button)) as Button;
        Button button = GameObject.Instantiate(temp) as Button;
        button.transform.SetParent(parent.transform, false);//.parent = parent.transform;


        if (layer.images != null)
        {
            for (int imageIndex = 0; imageIndex < layer.images.Length; imageIndex++)
            {
                PSDUI.Image image = layer.images[imageIndex];

                if (image.name.Contains("normal"))
                {
                    if (image.imageSource == PSDUI.ImageSource.Custom)
                    {
                        string assetPath = baseDirectory + image.name + PSDImporterConst.PNG_SUFFIX;
                        Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
                        button.image.sprite = sprite;

                        RectTransform rectTransform = button.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(image.size.width, image.size.height);
                        rectTransform.anchoredPosition = new Vector2(image.position.x, image.position.y);
                    }
                }
                else if (image.name.Contains("Title"))
                {
                    if (image.imageType == PSDUI.ImageType.Label)
                    {
                        //�������� 
                        Text text = button.GetComponentInChildren<Text>();
                        Color color;
                        if (UnityEngine.ColorUtility.TryParseHtmlString(("#" + image.arguments[0]), out color))
                        {
                            text.color = color;
                        }

                        int size;
                        if (int.TryParse(image.arguments[2], out size))
                        {
                            text.fontSize = size;
                        }

                        text.text = System.Text.UTF8Encoding.UTF8.GetString(System.Text.ASCIIEncoding.UTF8.GetBytes(image.arguments[3]));
                    }
                }
            }
        }
    }

    static private Toggle DrawToggle(PSDUI.Layer layer, GameObject parent)
    {
        Toggle temp = Resources.Load(PSDImporterConst.PREFAB_PATH_TOGGLE, typeof(Toggle)) as Toggle;
        Toggle toggle = GameObject.Instantiate(temp) as Toggle;
        toggle.transform.SetParent(parent.transform, false);//.parent = parent.transform;


        if (layer.images != null)
        {
            for (int imageIndex = 0; imageIndex < layer.images.Length; imageIndex++)
            {
                PSDUI.Image image = layer.images[imageIndex];

                if (image.name.Contains("normal"))
                {
                    if (image.imageSource == PSDUI.ImageSource.Custom)
                    {
                        string assetPath = baseDirectory + image.name + PSDImporterConst.PNG_SUFFIX;
                        Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
                        toggle.image.sprite = sprite;

                        RectTransform rectTransform = toggle.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(image.size.width, image.size.height);
                        rectTransform.anchoredPosition = new Vector2(image.position.x, image.position.y);
                    }
                }
            }
        }
        return toggle;
    }

    static private GridLayoutGroup DrawGrid(PSDUI.Layer layer, GameObject parent)
    {
        GridLayoutGroup temp = Resources.Load(PSDImporterConst.PREFAB_PATH_GRID, typeof(GridLayoutGroup)) as GridLayoutGroup;
        GridLayoutGroup gridLayoutGroup = GameObject.Instantiate(temp) as GridLayoutGroup;
        gridLayoutGroup.transform.SetParent(parent.transform, false);//.parent = parent.transform;

        gridLayoutGroup.padding = new RectOffset();
        gridLayoutGroup.cellSize = new Vector2(System.Convert.ToInt32(layer.arguments[2]), System.Convert.ToInt32(layer.arguments[3]));

        RectTransform rectTransform = gridLayoutGroup.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(layer.size.width, layer.size.height);
        rectTransform.anchoredPosition = new Vector2(layer.position.x, layer.position.y);

        int cellCount = System.Convert.ToInt32(layer.arguments[0]) * System.Convert.ToInt32(layer.arguments[1]);
        for (int cell = 0; cell < cellCount; cell++)
        {
            Image pic = Resources.Load(PSDImporterConst.PREFAB_PATH_IMAGE, typeof(Image)) as Image;
            pic.sprite = null;
            //            Sprite sprite = Resources.Load (relativeResoucesDirectory + "normal_13", typeof(Sprite)) as Sprite;
            //            pic.sprite = sprite;

            Image myImage = GameObject.Instantiate(pic) as Image;
            myImage.transform.SetParent(rectTransform, false); //parent = rectTransform;
        }
        return gridLayoutGroup;
    }

    static private void DrawScrollView(PSDUI.Layer layer, GameObject parent)
    {
        ScrollRect temp = Resources.Load(PSDImporterConst.PREFAB_PATH_SCROLLVIEW, typeof(ScrollRect)) as ScrollRect;
        ScrollRect scrollRect = GameObject.Instantiate(temp) as ScrollRect;
        scrollRect.transform.SetParent(parent.transform, false); //parent = parent.transform;

        RectTransform rectTransform = scrollRect.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(layer.size.width, layer.size.height);
        rectTransform.anchoredPosition = new Vector2(layer.position.x, layer.position.y);

        if (layer.layers != null)
        {
            PSDUI.Layer childLayer = layer.layers[0];
            string type = layer.arguments[0].ToUpper();
            switch (type)
            {
                case "V":
                    scrollRect.vertical = true;
                    scrollRect.horizontal = false;
                    break;
                case "H":
                    scrollRect.vertical = false;
                    scrollRect.horizontal = true;
                    break;
                default:
                    break;
            }

            DrawChildLayer(layer, scrollRect.content.gameObject);
        }
    }

    static private void DrawPanel(PSDUI.Layer layer, GameObject parent)
    {
        Image temp = Resources.Load(PSDImporterConst.PREFAB_PATH_IMAGE, typeof(Image)) as Image;
        Image panel = GameObject.Instantiate(temp) as Image;
        panel.transform.SetParent(parent.transform, false); //parent = parent.transform;

        panel.name = layer.name;

        DrawChildLayer(layer, panel.gameObject);

        PSDUI.Image image = layer.images[0];

        string assetPath = baseDirectory + image.name + PSDImporterConst.PNG_SUFFIX;
        Sprite sprite = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Sprite)) as Sprite;
        panel.sprite = sprite;

        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(image.size.width, image.size.height);
        rectTransform.anchoredPosition = new Vector2(image.position.x, image.position.y);
    }

    static private void DrawLayer(PSDUI.Layer layer, GameObject parent)
    {
        switch (layer.type)
        {
            case PSDUI.LayerType.Panel:
                DrawPanel(layer, parent);
                break;
            case PSDUI.LayerType.Normal:
                DrawNormalLayer(layer, parent);
                break;
            case PSDUI.LayerType.Button:
                DrawButton(layer, parent);
                break;
            case PSDUI.LayerType.Toggle:
                DrawToggle(layer, parent);
                break;
            case PSDUI.LayerType.Grid:
                DrawGrid(layer, parent);
                break;
            case PSDUI.LayerType.ScrollView:
                DrawScrollView(layer, parent);
                break;
        }
    }

    static private object DeserializeXml(string filePath, System.Type type)
    {
        object instance = null;
        StreamReader xmlFile = File.OpenText(filePath);
        if (xmlFile != null)
        {
            string xml = xmlFile.ReadToEnd();
            if ((xml != null) && (xml.ToString() != ""))
            {
                XmlSerializer xs = new XmlSerializer(type);
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] byteArray = encoding.GetBytes(xml);
                MemoryStream memoryStream = new MemoryStream(byteArray);
                XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);
                if (xmlTextWriter != null)
                {
                    instance = xs.Deserialize(memoryStream);
                }
            }
        }
        xmlFile.Close();
        return instance;
    }
}