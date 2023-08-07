using UnityEditor;
using UnityEngine;
using Unity.Barracuda;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Globalization;
using System.Text;
using UnityEngine.Networking;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
using UnityEditor.Experimental.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif
using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;


namespace AiKodex
{

    public class AifyEditor : EditorWindow
    {
        public enum Engine
        {
            aiFyV1,
            aiFyV2,
            aiFyV2_1,
            aiFyXL,
            aiFyXL_0_9

        };
        public static Engine engine = Engine.aiFyV1;
        string prompt = "";
        string[] examplePrompts = new string[]
        {
            "Shopping mall, city, sunshine, pixel art",
            "Japanese classroom scene, wooden table chair, tiled floor, green blackboard, teacher, students, pixel art",
            "Axe, sword, mining, character, various assets, pixel art, 8 bit, game design, sprite sheet",
            "Farm isometric, pixel art, detailed, retro design, green, vibrant",
            "car high speed, intense, dynamic, detailed, cartoon style, wide angled, overhead view, vibrant colors, whimsical, absurd, surreal, fun, isometric, low poly, 123rf, gettyimages, illustrator, photoshop",
            "Anime girl, bikini, full body, full chest, curvy, toned abs, minimalistic background, anime waifu, beautiful, good looking girl, anime style, Japanese, manga, otaku",
            "Japanese Beach, blue water, sunny, children, crowded, enjoying, splashes, manga, bright, vibrant, day time, Japan children, 4k",
            "darth vader, standing pose, barren land, greyscale, full body, metallic, render, vray, octane render, 3d model",
            "Vintage sports car with classic curves, captured in a moody, low key light with selective focus on the grille, classic, retro, moody, detailed, 3D model, artstation, blender render, 3ds max render",
            "Game design, cyberpunk style, 3D models",
            "A jungle city, with vines and roots serving as roads and buildings made of leaves, colorful, detailed, natural, tropical",
            "Lonely lighthouse on a rocky coast during a storm, with waves crashing and lightning flashing, moody, atmospheric, seascape, high detail",
            "Powerful sorceress, flowing robes and mystical staff, standing in dark and ominous forest, mysterious, detailed, high detail, fantasy portrait",
            "Editorial Style Photo, Bonsai Apple Tree, Task Lighting, Inspiring and Awesome, Sunset, Afternoon, Beautiful, Symmetric, 4k",
            "Rustic kitchen with exposed brick wall, reclaimed wood cabinetry, large farmhouse sink, industrial lighting fixtures, antique baking tools on open shelving, cast iron cookware, vintage accents, warm and inviting, detailed textures",
            "Luxurious bathroom with freestanding bathtub, rain shower, heated flooring, marble tiles, brass fixtures, floating vanity with double sink, elegant chandelier, high contrast lighting, spa-like atmosphere, high resolution",
            "Skilled archer, bow and quiver of arrows, standing in forest clearing, intense, detailed, high detail, portrait",
            "A medieval town with disco lights and a fountain, by Josef Thoma, matte painting trending on artstation HQ, concept art",
            "Majestic dragon, perched atop a cliff overlooking a fiery landscape, with smoke and ash rising into the air, intense, detailed, scales, dynamic, epic"
        };
        string negativePrompt = "";
        static NNModel upscaleModel;
        static Model runtimeModel;
        static IWorker worker;
        static Texture2D inputTexture, outputTexture;
        static RenderTexture outputRenderTexture;

        //Concept Art
        string search = "";
        public enum Magnification
        {
            quarter,
            half,
            x2,
            x4
        };
        static float magnificationAmount;
        static string magnificationTag;
        public enum ModelType
        {
            ESRGAN_Server,
            LightWeightSuperResolution,
            HeavyWeightSuperResolution
        };
        public static ModelType _modelType = ModelType.ESRGAN_Server;
        public static Magnification _magnification = Magnification.x4;
        public static bool powerOfTwo = true;

        public static Texture2D sourceImage;
        public static IEnumerable<Texture> selectedTextures;
        private static int conceptArtImageNumber = 5;
        private static int normalStrength = 5; // default 5
        private static bool upscaleGroupEnabled;
        private static bool normalGroupEnabled;
        private static bool specularGroupEnabled;
        private static bool depthGroupEnabled;
        private static float specularCutOff = 0.40f; // default 0.4
        private static float specularContrast = 1.5f; // default 1.5
        private static string appName = "Aify UI";
        public static string normalSuffix = "_normal.png";
        public static string specularSuffix = "_specular.png";
        public static string depthSuffix = "_depth.png";
        public static bool running = false;
        float aspectRatio;
        static Dictionary<string, bool> s_UIHelperFoldouts = new Dictionary<string, bool>();
        private List<string> m_Inputs = new List<string>();
        private List<string> m_Details = new List<string>();
        private Vector2 mainScroll, conceptArtScroll;
        private Vector2 m_InputsScrollPosition = Vector2.zero;
        private Vector2 m_InputsScrollPositionMapDetails = Vector2.zero;
        IEnumerable<Texture> prevSelectedTextures, currentSelectedTextures;
        byte[] encJpg;
        string base64encJpg, resultFromServer, sDb64FromServer;
        string seed = "1";
        bool autoRandomizeSeed = true;
        float img2imgstrength = 0.5f;
        Vector2Int img_dim = new Vector2Int(512, 512);
        Vector2Int dim_index = new Vector2Int(0, 0);
        string[] options = new string[]
        {
            "512", "448", "384", "320", "256"
        };
        string[] XL0_9_options = new string[]
        {
            "1024"
        };
        int inferenceSteps = 30;
        int cfgScale = 8;
        public enum Sampler
        {
            k_euler_a,
            k_euler,
            k_lms,
            ddim,
            plms,
            k_huen,
            k_euler_ancestral,
            k_dpm_2_ancestral,
            k_dpmpp_2s_ancestral,
            k_dpmpp_2m
        };
        public static Sampler sampler = Sampler.k_euler_a;
        public Texture2D initImage, forUpscale;
        private string _directoryPath = "";
        bool autoPath = true;
        bool previewInInspector = true;
        float zoomPreview = 0.8f;
        bool fitGrid = true;
        float conceptPreview = 0.3f;
        private Vector2 _scrollPosition = Vector2.zero;
        private bool initDone = false;
        private GUIStyle StatesLabel;
        List<Texture2D> temp;
        List<string> promptsConceptArt;
        float postProgress, postUpscaleProgress;
        bool postFlag, postUpscaleFlag;
        public enum Mask
        {
            none,
            centralIconMask

        };
        public static Mask mask = Mask.none;
        void InitStyles()
        {
            initDone = true;
            StatesLabel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                margin = new RectOffset(),
                padding = new RectOffset(),
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
        }
        void Awake()
        {
#if UNITY_2022_1_OR_NEWER
            string unityVersion = Application.unityVersion;
            int majorVersion = int.Parse(unityVersion.Split('.')[0]);
            if (majorVersion >= 2022)
            {
                PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
            }
#endif
        }

        // create menu item and window
        [MenuItem("Window/Aify Editor")]
        static void Init()
        {
            AifyEditor window = (AifyEditor)EditorWindow.GetWindow(typeof(AifyEditor));
            window.titleContent.text = appName;
            window.minSize = new Vector2(500, 500);
            running = true;
        }

        // window closed
        void OnDestroy()
        {
            running = false;
        }
        private static void ListUIHelper(string sectionTitle, List<string> names, ref Vector2 scrollPosition, float maxHeightMultiplier = 1f)
        {
            int n = names.Count();
            GUILayout.Space(10);
            if (!s_UIHelperFoldouts.TryGetValue(sectionTitle, out bool foldout))
                foldout = true;

            foldout = EditorGUILayout.Foldout(foldout, sectionTitle, true, EditorStyles.foldoutHeader);
            s_UIHelperFoldouts[sectionTitle] = foldout;
            if (foldout)
            {
                float height = Mathf.Min(n * 20f + 2f, 150f * maxHeightMultiplier);
                if (n == 0)
                    return;

                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUI.skin.box, GUILayout.MinHeight(height));
                Event e = Event.current;
                float lineHeight = 16.0f;

                StringBuilder fullText = new StringBuilder();
                fullText.Append(sectionTitle);
                fullText.AppendLine();
                for (int i = 0; i < n; ++i)
                {
                    string name = names[i];
                    fullText.Append($"{name}");
                    fullText.AppendLine();
                }

                for (int i = 0; i < n; ++i)
                {
                    Rect r = EditorGUILayout.GetControlRect(false, lineHeight);

                    string name = names[i];

                    // Context menu, "Copy"
                    if (e.type == EventType.ContextClick && r.Contains(e.mousePosition))
                    {
                        e.Use();
                        var menu = new GenericMenu();

                        // need to copy current value to be used in delegate
                        // (C# closures close over variables, not their values)
                        menu.AddItem(new GUIContent($"Copy current line"), false, delegate
                        {
                            EditorGUIUtility.systemCopyBuffer = $"{name}";
                        });
                        menu.AddItem(new GUIContent($"Copy section"), false, delegate
                        {
                            EditorGUIUtility.systemCopyBuffer = fullText.ToString();
                        });
                        menu.ShowAsContext();
                    }

                    // Color even line for readability
                    if (e.type == EventType.Repaint)
                    {
                        GUIStyle st = "CN EntryBackEven";
                        if ((i & 1) == 0)
                            st.Draw(r, false, false, false, false);
                    }

                    // layer name on the right side
                    Rect locRect = r;
                    locRect.xMax = locRect.xMin;
                    GUIContent gc = new GUIContent(name.ToString(CultureInfo.InvariantCulture));

                    // calculate size so we can left-align it
                    Vector2 size = EditorStyles.miniBoldLabel.CalcSize(gc);
                    locRect.xMax += size.x;
                    GUI.Label(locRect, gc, EditorStyles.miniBoldLabel);
                    locRect.xMax += 2;
                }

                GUILayout.EndScrollView();
            }

        }

        // main loop/gui
        void OnGUI()
        {
            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);
            if (!initDone)
                InitStyles();
            GUIStyle sectionTitle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            GUIStyle headStyle = new GUIStyle("Box");
            headStyle.fontSize = 24;
            headStyle.normal.textColor = Color.white;
            EditorGUILayout.BeginHorizontal();
            Texture logo = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Aify/Editor/Aify Logo.png", typeof(Texture));
            Texture info = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Aify/Editor/info.png", typeof(Texture));
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("      Aify ", headStyle);
            EditorGUILayout.EndVertical();
            GUI.DrawTexture(new Rect(5, 2, 40, 40), logo, ScaleMode.StretchToFill, true, 10.0F);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField("Generator", sectionTitle);
            EditorGUILayout.Space(20);
            engine = (Engine)EditorGUILayout.EnumPopup(new GUIContent("Engine", info, "Engines are SD models trained on game textures and concept LoRAs\nAiFy V1 based on SD 1.5\nAiFy V2 based on SD 2.0\nAiFy V2_1 based on SD 2.1\nAiFy XL based on SDXL Beta"), engine);
            EditorGUILayout.LabelField(new GUIContent("Prompt", info, "Enter text that you want to convert to images"), EditorStyles.boldLabel);
            EditorStyles.textArea.wordWrap = true;
            prompt = EditorGUILayout.TextArea(prompt, EditorStyles.textArea, GUILayout.Height(40));
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(" Shuffle", EditorStyles.radioButton))
                prompt = examplePrompts[UnityEngine.Random.Range(0, examplePrompts.Length)];
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Negative Prompt", info, "Enter the description you don't wish to see in the generated image"), EditorStyles.boldLabel);
            EditorStyles.textArea.wordWrap = true;

            negativePrompt = EditorGUILayout.TextArea(negativePrompt, EditorStyles.textArea, GUILayout.Height(20));
            EditorGUILayout.Space(10);


            GUIStyle secondary = new GUIStyle("WhiteLargeLabel");
            EditorGUILayout.LabelField("Settings", secondary);
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (engine == Engine.aiFyXL_0_9)
            {
                EditorGUILayout.LabelField("Width", GUILayout.MaxWidth(40));
                dim_index.x = EditorGUILayout.Popup(0, XL0_9_options, EditorStyles.popup);
                EditorGUILayout.LabelField("Height", GUILayout.MaxWidth(40));
                dim_index.y = EditorGUILayout.Popup(0, XL0_9_options, EditorStyles.popup);
            }
            else
            {
                EditorGUILayout.LabelField("Width", GUILayout.MaxWidth(40));
                dim_index.x = EditorGUILayout.Popup(dim_index.x, options, EditorStyles.popup);
                EditorGUILayout.LabelField("Height", GUILayout.MaxWidth(40));
                dim_index.y = EditorGUILayout.Popup(dim_index.y, options, EditorStyles.popup);
                img_dim.x = int.Parse(options[dim_index.x]);
                img_dim.y = int.Parse(options[dim_index.y]);
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Button("", GUILayout.Width(5), GUILayout.Height(100));
            GUILayout.Button("Aspect\nRatio", GUILayout.Width((float)img_dim.x / (float)img_dim.y * 100f), GUILayout.Height(100));
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUI.BeginDisabledGroup(true);
            GUILayout.Button("", GUILayout.Width(5), GUILayout.Height(5));
            GUILayout.Button("", GUILayout.Width((float)img_dim.x / (float)img_dim.y * 100f), GUILayout.Height(5));
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndVertical();


            inferenceSteps = EditorGUILayout.IntSlider(new GUIContent("Steps", info, "Number of steps of redefinition performed by the model"), inferenceSteps, 10, 50);
            cfgScale = EditorGUILayout.IntSlider(new GUIContent("Prompt Strength", info, "Level of adherence of the engine's output to the provided prompt"), cfgScale, 0, 20);
            sampler = (Sampler)EditorGUILayout.EnumPopup(new GUIContent("Sampler Model", info, "The sampler model refers to minor differences in training data"), sampler);


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent($"Seed: {seed}", info, "Refers to the seed value for random latent noise generation. Seed is deterministic which means that the same seed will give the same image if all the other parameters are the same. Default value is 1. Auto Boolean randomizes the seed when Generate Image is clicked. Randomize variable randomizes the seed on demand. It also sets the name of the texture so it is unique. If auto is deselected, then the image overwrites which is sometimes desirable and convenient. If this does not suit you, please rename the texture before clicking on generate if auto is set to false."));
            seed = EditorGUILayout.TextField(seed);
            autoRandomizeSeed = EditorGUILayout.ToggleLeft("Auto", autoRandomizeSeed, GUILayout.MaxWidth(50));
            if (GUILayout.Button("Randomize", GUILayout.ExpandWidth(false), GUILayout.Width(80)))
                seed = UnityEngine.Random.Range(100000, 999999).ToString();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            initImage = EditorGUILayout.ObjectField(new GUIContent("Match Image", info, "Image that is used to guide the generation process. This results in an image similar to the one specified."), initImage, typeof(Texture2D), false, GUILayout.Height(70), GUILayout.Width(70), GUILayout.MaxWidth(220)) as Texture2D;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(12);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
                initImage = null;
            if (GUILayout.Button("Last", GUILayout.Width(50)))
                initImage = sourceImage;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(" ", $" ({img_dim.x} x {img_dim.y})");
            img2imgstrength = EditorGUILayout.Slider(new GUIContent("Match Image Strength", info, "Determines how closely the image in the Match Image input is matched."), img2imgstrength, 0, 1);
            mask = (Mask)EditorGUILayout.EnumPopup(new GUIContent("Mask", info, "When this parameter is set to Central Icon Mask, the central portion of the image in the Match Image input chnages according to the prompt. The periphery of the input image is kept the same."), mask);


            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(autoPath == true);
            if (autoPath)
                _directoryPath = EditorGUILayout.TextField("Textures Folder", "Assets/Aify");
            else
                _directoryPath = EditorGUILayout.TextField("Textures Folder", _directoryPath);
            if (GUILayout.Button(". . /", GUILayout.MaxWidth(50)))
                _directoryPath = EditorUtility.OpenFolderPanel("", "", "");
            EditorGUI.EndDisabledGroup();
            autoPath = EditorGUILayout.ToggleLeft("Auto", autoPath, GUILayout.MaxWidth(50));
            EditorGUILayout.EndHorizontal();

            GUI.enabled = prompt == null || prompt == "" || postFlag ? false : true;
            if (GUILayout.Button("Generate Image", GUILayout.Height(30)))
            {
                GenerateImage();
                postFlag = true;
                postProgress = 0;
                if (autoRandomizeSeed)
                    seed = UnityEngine.Random.Range(100000, 999999).ToString();
            }

            GUI.enabled = true;
            Rect loading = GUILayoutUtility.GetRect(9, 9);
            if (postFlag)
            {
                Repaint();
                EditorGUI.ProgressBar(loading, Mathf.Sqrt(++postProgress) * 0.01f, "");
            }

            EditorGUILayout.BeginHorizontal();
            previewInInspector = EditorGUILayout.Toggle("Preview Selected Image", previewInInspector, GUILayout.ExpandWidth(true));
            EditorGUI.BeginDisabledGroup(previewInInspector == false);
            zoomPreview = EditorGUILayout.Slider(zoomPreview, 0.3f, 0.95f, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (previewInInspector && Selection.activeObject != null && Selection.activeObject.GetType().Equals(typeof(Texture2D)))
                GUILayout.Box((Texture2D)Selection.activeObject, GUILayout.Width(position.width * zoomPreview), GUILayout.Height(position.width * zoomPreview), GUILayout.ExpandWidth(true));

            EditorGUILayout.Space(10);
            GUILayout.EndVertical();

            EditorGUILayout.Space(20);

            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField("Concept Art Browser", sectionTitle);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Search Terms", EditorStyles.boldLabel);

            search = EditorGUILayout.TextArea(search, EditorStyles.textArea, GUILayout.Height(20));
            conceptArtImageNumber = EditorGUILayout.IntSlider("Images to display", conceptArtImageNumber, 1, 50);
            EditorGUILayout.BeginHorizontal();
            fitGrid = EditorGUILayout.Toggle("Fit Grid", fitGrid, GUILayout.ExpandWidth(true));
            EditorGUI.BeginDisabledGroup(fitGrid == true);
            conceptPreview = EditorGUILayout.Slider(conceptPreview, 0.2f, 1, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            GUI.enabled = search == null || search == "" ? false : true;
            if (GUILayout.Button("Browse Concept Art", GUILayout.Height(30)))
            {
                temp = ConceptArt();
                promptsConceptArt = PromptsConceptArt();
            }
            GUI.enabled = true;
            if (temp != null)
            {
                if (fitGrid)
                {
                    int ConceptWindowWidth = (int)position.width / 160;
                    int lineChange = 0;
                    for (int i = 0; i < temp.Count(); i++)
                        if (i % ConceptWindowWidth == 0)
                            lineChange++;

                    conceptArtScroll = EditorGUILayout.BeginScrollView(conceptArtScroll, GUILayout.Height(250 * lineChange));
                    EditorGUILayout.BeginVertical();
                    for (int i = 0; i < temp.Count(); i++)
                    {
                        if (i % ConceptWindowWidth == 0)
                        {
                            EditorGUILayout.BeginHorizontal();
                        }
                        EditorGUILayout.BeginVertical();
                        GUILayout.Box(temp[i], GUILayout.Width(150), GUILayout.Height(150), GUILayout.ExpandWidth(true));
                        GUILayout.Label($"Prompt: \"{promptsConceptArt[i]}\"", EditorStyles.wordWrappedLabel);
                        if (GUILayout.Button("Copy Prompt")) prompt = promptsConceptArt[i];

                        if (GUILayout.Button("Save"))
                        {
                            Texture2D tex = temp[i];
                            File.WriteAllBytes($"{_directoryPath}/{search}_{i}.png", tex.EncodeToPNG());
                            AssetDatabase.Refresh();
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath($"{_directoryPath}/{seed}.png");

                        }
                        EditorGUILayout.EndVertical();
                        if (i != 0 && i % ConceptWindowWidth == ConceptWindowWidth - 1)
                            EditorGUILayout.EndHorizontal();
                    }
                    if (temp.Count() % ConceptWindowWidth < ConceptWindowWidth && temp.Count() % ConceptWindowWidth != 0)
                        EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndScrollView();
                }
                else
                {

                    conceptArtScroll = EditorGUILayout.BeginScrollView(conceptArtScroll, GUILayout.Height(520 * System.Convert.ToInt32(temp.Count() != 0) * conceptPreview));
                    EditorGUILayout.BeginHorizontal();
                    for (int i = 0; i < temp.Count(); i++)
                    {
                        EditorGUILayout.BeginVertical();
                        GUILayout.Box(temp[i], GUILayout.Width(position.width * conceptPreview), GUILayout.Height(position.width * conceptPreview), GUILayout.ExpandWidth(true));
                        if (GUILayout.Button("Save"))
                        {
                            Texture2D tex = temp[i];
                            File.WriteAllBytes($"{_directoryPath}/{search}_{i}.png", tex.EncodeToPNG());
                            AssetDatabase.Refresh();
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath($"{_directoryPath}/{seed}.png");

                        }
                        EditorGUILayout.EndVertical();

                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndScrollView();


                }
            }

            if (temp != null)
            {
                GUI.enabled = temp.Count != 0;
                if (GUILayout.Button("Clear", GUILayout.Height(20)))
                {
                    temp.Clear();
                }
                GUI.enabled = true;
            }
            EditorGUILayout.Space(10);
            GUILayout.EndVertical();

            EditorGUILayout.Space(20);

            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField("Upscaler Settings", sectionTitle);
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            forUpscale = EditorGUILayout.ObjectField("Image to Upscale", forUpscale, typeof(Texture2D), false, GUILayout.Height(70), GUILayout.Width(70), GUILayout.MaxWidth(220)) as Texture2D;
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(12);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
                forUpscale = null;
            if (GUILayout.Button("Last", GUILayout.Width(50)))
                forUpscale = sourceImage;
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            _modelType = (ModelType)EditorGUILayout.EnumPopup("Select Neural Network", _modelType);
            EditorGUI.BeginDisabledGroup(_modelType == ModelType.ESRGAN_Server);
            if (_modelType == ModelType.ESRGAN_Server)
            {
                _magnification = Magnification.x2;
                _magnification = (Magnification)EditorGUILayout.EnumPopup("Magnification Factor", _magnification);
            }
            else _magnification = (Magnification)EditorGUILayout.EnumPopup("Magnification Factor", _magnification);
            powerOfTwo = EditorGUILayout.Toggle("Round dimensions", powerOfTwo);
            EditorGUI.EndDisabledGroup();
            GUI.enabled = forUpscale != null && forUpscale.width * forUpscale.height <= 1048576 && !postUpscaleFlag ? true : false;
            if (GUILayout.Button("Upscale Image", GUILayout.Height(30)))
            {
                string path = AssetDatabase.GetAssetPath(forUpscale);
                if (_modelType == ModelType.LightWeightSuperResolution || _modelType == ModelType.HeavyWeightSuperResolution)
                {
                    TextureImporter initImporter = (TextureImporter)TextureImporter.GetAtPath(path);
                    initImporter.sRGBTexture = false;
                    initImporter.SaveAndReimport();
                    inputTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
                    TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                    switch (_modelType)
                    {
                        case ModelType.LightWeightSuperResolution:
                            runtimeModel = ModelLoader.Load((NNModel)AssetDatabase.LoadAssetAtPath("Assets/Aify/Neural Network Models/LWSR.onnx", typeof(NNModel)));
                            break;
                        case ModelType.HeavyWeightSuperResolution:
                            runtimeModel = ModelLoader.Load((NNModel)AssetDatabase.LoadAssetAtPath("Assets/Aify/Neural Network Models/HWSR.onnx", typeof(NNModel)));
                            break;

                    }
                    switch (_magnification)
                    {
                        case Magnification.quarter:
                            magnificationAmount = 0.125f;
                            magnificationTag = "_xquarter";
                            break;
                        case Magnification.half:
                            magnificationAmount = 0.25f;
                            magnificationTag = "_xhalf";
                            break;
                        case Magnification.x2:
                            magnificationAmount = 0.5f;
                            magnificationTag = "_x2";
                            break;
                        case Magnification.x4:
                            magnificationAmount = 1;
                            magnificationTag = "_x4";
                            break;
                    }


                    worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

                    var input = new Tensor(inputTexture, 3);

                    worker.Execute(input);

                    Tensor output = worker.PeekOutput("output");

                    outputRenderTexture = new RenderTexture(inputTexture.width * 4, inputTexture.height * 4, 16, RenderTextureFormat.ARGB32);
                    outputRenderTexture.Create();

                    output.ToRenderTexture(outputRenderTexture);
                    RenderTexture.active = outputRenderTexture;
                    outputTexture = new Texture2D(outputRenderTexture.width, outputRenderTexture.height);
                    outputTexture.ReadPixels(new Rect(0, 0, outputRenderTexture.width, outputRenderTexture.height), 0, 0);
                    outputTexture.Apply();
                    // Texture Resize
                    TextureScale.Bilinear(outputTexture, (int)(outputTexture.width * magnificationAmount), (int)(outputTexture.height * magnificationAmount));
                    File.WriteAllBytes(path.Substring(0, path.Length - Path.GetExtension(path).Length) + magnificationTag + ".png", outputTexture.EncodeToPNG());
                    output.Dispose();
                    AssetDatabase.Refresh();
                    TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path.Substring(0, path.Length - Path.GetExtension(path).Length) + magnificationTag + ".png");
                    if (!powerOfTwo)
                    {
                        textureImporter.npotScale = TextureImporterNPOTScale.ToNearest;
                        importer.npotScale = TextureImporterNPOTScale.None;
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    }
                    initImporter.sRGBTexture = true;
                    importer.sRGBTexture = true;
                    importer.SaveAndReimport();
                    worker.Dispose();
                }
                else
                {
                    postUpscaleFlag = true;
                    postUpscaleProgress = 0;
                    TextureImporter initImporter = (TextureImporter)TextureImporter.GetAtPath(path);
                    initImporter.sRGBTexture = false;
                    initImporter.isReadable = true;
                    initImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    initImporter.SaveAndReimport();
                    encJpg = forUpscale.DeCompress().EncodeToJPG();
                    base64encJpg = Convert.ToBase64String(encJpg);
                    this.StartCoroutine(PostUpscale(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovLzM0LjE5Ny44MS4xODQ6NTAwMC9kYXRhCg==")), "{ \"engine\":\"" + "upscale" + "\",\"data\":\"" + "" + "\",\"neg\":\"" + "" + "\",\"width\":\"" + "0" + "\",\"height\":\"" + "0" + "\",\"steps\":\"" + "0" + "\",\"cfgScale\":\"" + "0" + "\",\"sampler\":\"" + "" + "\",\"seed\":\"" + "1" + "\",\"initImg\":\"" + $"{base64encJpg}" + "\",\"img2imgStrength\":\"" + "1" + "\",\"mask\":\"" + "" + "\"}"));
                }
            }
            GUI.enabled = true;
            Rect upscaleLoading = GUILayoutUtility.GetRect(9, 9);
            if (postUpscaleFlag)
            {
                Repaint();
                EditorGUI.ProgressBar(upscaleLoading, Mathf.Sqrt(++postUpscaleProgress) * 0.01f, "");
            }
            if (forUpscale != null && forUpscale.width * forUpscale.height > 1048576)
                EditorGUILayout.HelpBox("Selected Image has over 1048576 pixels (1024 x 1024). Please choose an image with or under the specified resolution for upscaling.", MessageType.Warning);
            EditorGUILayout.Space();
            GUILayout.EndVertical();

            EditorGUILayout.Space(20);


            GUILayout.BeginVertical("window");
            EditorGUILayout.LabelField("Texture Tools", sectionTitle);
            GUILayout.Label("Selected Textures");
            prevSelectedTextures = currentSelectedTextures;
            selectedTextures = Selection.GetFiltered(typeof(Texture), SelectionMode.Assets).Cast<Texture>();
            currentSelectedTextures = selectedTextures;

            if (selectedTextures.Count() > 0)
            {
                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(position.width));
                int windowWidth = (int)position.width / 75;
                for (int i = 0; i < selectedTextures.Count(); i++)
                {
                    if (i % windowWidth == 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                    }
                    sourceImage = EditorGUILayout.ObjectField(selectedTextures.ElementAt(i), typeof(Texture2D), false, GUILayout.Height(75), GUILayout.Width(75)) as Texture2D;
                    if (i != 0 && i % windowWidth == windowWidth - 1)
                        EditorGUILayout.EndHorizontal();

                }
                if (selectedTextures.Count() % windowWidth < windowWidth && selectedTextures.Count() % windowWidth != 0)
                    EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();

                if (prevSelectedTextures != currentSelectedTextures)
                {
                    m_Inputs.Clear();
                    for (int i = 0; i < selectedTextures.Count(); i++)
                    {
                        m_Inputs.Add((i + 1).ToString() + ". Name: " + selectedTextures.ElementAt(i).name + " | Dimensions: " + selectedTextures.ElementAt(i).width + "x" + selectedTextures.ElementAt(i).height + " | Format: " + selectedTextures.ElementAt(i).graphicsFormat);
                    }
                }

                ListUIHelper($"Info ({selectedTextures.Count()})", m_Inputs, ref m_InputsScrollPosition);

            }
            else
            {
                Rect rect = EditorGUILayout.BeginHorizontal();
                GUILayout.Label(" None", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
                GUI.Box(rect, GUIContent.none);

            }
            EditorGUILayout.Space();

            depthGroupEnabled = EditorGUILayout.BeginToggleGroup("Generate Depth Map", depthGroupEnabled);
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();

            normalGroupEnabled = EditorGUILayout.BeginToggleGroup("Generate Normal Map", normalGroupEnabled);
            normalStrength = EditorGUILayout.IntSlider("Strength", normalStrength, 1, 20);
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();

            specularGroupEnabled = EditorGUILayout.BeginToggleGroup("Generate Smoothness", specularGroupEnabled);
            specularCutOff = EditorGUILayout.Slider("Brightness Cutoff", specularCutOff, 0, 1);
            specularContrast = EditorGUILayout.Slider("Specular Contrast", specularContrast, 0, 2);
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();



            var texturesCount = Convert.ToInt32(depthGroupEnabled) + Convert.ToInt32(normalGroupEnabled) + Convert.ToInt32(specularGroupEnabled);
            var listNumber = 0;
            if (prevSelectedTextures != currentSelectedTextures)
            {
                m_Details.Clear();

                if (depthGroupEnabled)
                {
                    for (int i = 0; i < selectedTextures.Count(); i++)
                    {
                        listNumber++;
                        m_Details.Add((listNumber).ToString() + ". DEPTH TEXTURE | " + "Name: " + selectedTextures.ElementAt(i).name);
                    }
                }
                if (normalGroupEnabled)
                {
                    for (int i = 0; i < selectedTextures.Count(); i++)
                    {
                        listNumber++;
                        m_Details.Add((listNumber).ToString() + ". NORMAL TEXTURE | " + "Name: " + selectedTextures.ElementAt(i).name);
                    }
                }
                if (specularGroupEnabled)
                {
                    for (int i = 0; i < selectedTextures.Count(); i++)
                    {
                        listNumber++;
                        m_Details.Add((listNumber).ToString() + ". SPECULAR TEXTURE | " + "Name: " + selectedTextures.ElementAt(i).name);
                    }
                }
            }

            ListUIHelper($"Map Details ({selectedTextures.Count() * texturesCount})", m_Details, ref m_InputsScrollPositionMapDetails);


            //  ** Create button GUI **
            EditorGUILayout.Space();
            GUI.enabled = sourceImage; // disabled if no sourceImage selected
            EditorGUI.BeginDisabledGroup(depthGroupEnabled == false && normalGroupEnabled == false && specularGroupEnabled == false);
            if (GUILayout.Button(new GUIContent("Generate Textures"), GUILayout.Height(30)))
            {
                for (int i = 0; i < selectedTextures.Count(); i++)
                {
                    buildMaps(selectedTextures.ElementAt(i).name, i);
                }
            }
            EditorGUI.EndDisabledGroup();
            GUI.enabled = true;
            GUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }

        void GenerateImage()
        {
            if (initImage != null)
            {
                string path = AssetDatabase.GetAssetPath(initImage);
                TextureImporter initImporter = (TextureImporter)TextureImporter.GetAtPath(path);
                initImporter.sRGBTexture = false;
                initImporter.isReadable = true;
                initImporter.textureCompression = TextureImporterCompression.Uncompressed;
                initImporter.SaveAndReimport();
                Texture2D scaledInitImage = initImage;
                TextureScale.Bilinear(scaledInitImage, img_dim.x, img_dim.y);
                encJpg = scaledInitImage.DeCompress().EncodeToJPG();
                base64encJpg = Convert.ToBase64String(encJpg);

                if (mask == Mask.centralIconMask)
                    this.StartCoroutine(Post(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovLzM0LjIyNS4xNTkuOTo1MDAwL2RhdGE=")), "{\"engine\":\"" + $"{engine}" + "\",\"data\":\"" + $"{prompt}" + "\",\"neg\":\"" + $"{negativePrompt}" + "\",\"width\":\"" + $"{img_dim.x}" + "\",\"height\":\"" + $"{img_dim.y}" + "\",\"steps\":\"" + $"{inferenceSteps}" + "\",\"cfgScale\":\"" + $"{cfgScale}" + "\",\"sampler\":\"" + $"{sampler}" + "\",\"seed\":\"" + $"{seed}" + "\",\"initImg\":\"" + $"{base64encJpg}" + "\",\"img2imgStrength\":\"" + $"{img2imgstrength}" + "\",\"mask\":\"" + $"{mask}" + "\"}"));
                else
                    this.StartCoroutine(Post(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovLzM0LjIyNS4xNTkuOTo1MDAwL2RhdGE=")), "{\"engine\":\"" + $"{engine}" + "\",\"data\":\"" + $"{prompt}" + "\",\"neg\":\"" + $"{negativePrompt}" + "\",\"width\":\"" + $"{img_dim.x}" + "\",\"height\":\"" + $"{img_dim.y}" + "\",\"steps\":\"" + $"{inferenceSteps}" + "\",\"cfgScale\":\"" + $"{cfgScale}" + "\",\"sampler\":\"" + $"{sampler}" + "\",\"seed\":\"" + $"{seed}" + "\",\"initImg\":\"" + $"{base64encJpg}" + "\",\"img2imgStrength\":\"" + $"{img2imgstrength}" + "\",\"mask\":\"" + "" + "\"}"));
                initImporter.sRGBTexture = true;
                initImporter.SaveAndReimport();
            }
            else
                this.StartCoroutine(Post(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovLzM0LjIyNS4xNTkuOTo1MDAwL2RhdGE=")), "{\"engine\":\"" + $"{engine}" + "\",\"data\":\"" + $"{prompt}" + "\",\"neg\":\"" + $"{negativePrompt}" + "\",\"width\":\"" + $"{img_dim.x}" + "\",\"height\":\"" + $"{img_dim.y}" + "\",\"steps\":\"" + $"{inferenceSteps}" + "\",\"cfgScale\":\"" + $"{cfgScale}" + "\",\"sampler\":\"" + $"{sampler}" + "\",\"seed\":\"" + $"{seed}" + "\",\"initImg\":\"" + "\",\"img2imgStrength\":\"" + "\",\"mask\":\"" + "" + "\"}"));

        }

        List<Texture2D> ConceptArt()
        {
            List<Texture2D> conceptArtTextureArray = new List<Texture2D>();
            string jsonString = Get(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovLzM0LjIyNS4xNTkuOTo1MDAwL2RhdGE=")) + search);
            List<string> srcSmallUrls = new List<string>();
            int index = 0;
            while ((index = jsonString.IndexOf("\"srcSmall\":", index)) != -1)
            {
                index = index + "\"srcSmall\":".Length;
                int endIndex = jsonString.IndexOf(",", index);
                if (endIndex == -1)
                {
                    endIndex = jsonString.IndexOf("}", index);
                }
                string url = jsonString.Substring(index, endIndex - index).Trim().Trim('"');
                srcSmallUrls.Add(url);
            }
            for (int i = 0; i < conceptArtImageNumber; i++)
            {
                byte[] imageBytes = DownloadConceptArtImage(srcSmallUrls.ElementAt(i));
                outputRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                outputRenderTexture.Create();
                outputTexture = new Texture2D(outputRenderTexture.width, outputRenderTexture.height);
                outputTexture.LoadImage(imageBytes);
                outputTexture.Apply();
                conceptArtTextureArray.Add(outputTexture);
            }
            return conceptArtTextureArray;
        }
        List<string> PromptsConceptArt()
        {
            List<string> conceptArtPrompts = new List<string>();
            string jsonString = Get(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9sZXhpY2EuYXJ0L2FwaS92MS9zZWFyY2g/cT0=")) + search);
            List<string> prompts = new List<string>();
            int index = 0;
            while ((index = jsonString.IndexOf("\"prompt\":", index)) != -1)
            {
                index = index + "\"prompt\":".Length;
                int endIndex = jsonString.IndexOf(",", index);
                if (endIndex == -1)
                {
                    endIndex = jsonString.IndexOf("}", index);
                }
                string url = jsonString.Substring(index, endIndex - index).Trim().Trim('"');
                prompts.Add(url);
            }

            return prompts;
        }
        public Byte[] DownloadConceptArtImage(string url)
        {
            UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SendWebRequest();
            while (!request.isDone)
            {
                //Timeout Code
            }
            if (request.responseCode.ToString() != "200")
            {
                return null;
            }
            else
            {
                return request.downloadHandler.data;
            }

        }


        void buildMaps(string baseFile, int index)
        {

            float progress = 0.0f;
            bool setReadable = false;

            // check if its readable, if not set it temporarily readable
            string path = AssetDatabase.GetAssetPath(selectedTextures.ElementAt(index));
            TextureImporter initImporter = (TextureImporter)TextureImporter.GetAtPath(path);
            initImporter.sRGBTexture = false;
            initImporter.SaveAndReimport();
            inputTexture = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));
            TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter.isReadable == false)
            {
                textureImporter.isReadable = true;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                setReadable = true;
            }
            if (!powerOfTwo)
            {
                textureImporter.npotScale = TextureImporterNPOTScale.None;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            if (depthGroupEnabled)
            {

                encJpg = inputTexture.DeCompress().EncodeToJPG();
                base64encJpg = Convert.ToBase64String(encJpg);

                this.StartCoroutine(PostDepth(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cDovLzU0LjE2MS4xMTUuNzM6NTAwMC9kYXRh")), "{\"data\":\"" + $"{base64encJpg}" + "\"}"));

            }


            float progressStep = 1.0f / inputTexture.height;
            Texture2D texSource = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGB24, false, false);
            // clone original texture
            Color[] temp = inputTexture.GetPixels();
            texSource.SetPixels(temp);
            if (specularGroupEnabled)
            {
                Texture2D texSpecular = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGB24, false, false);
                Color[] pixels = new Color[inputTexture.width * inputTexture.height];
                for (int y = 0; y < inputTexture.height; y++)
                {
                    for (int x = 0; x < inputTexture.width; x++)
                    {
                        float bw = inputTexture.GetPixel(x, y).grayscale;
                        // adjust contrast
                        bw *= bw * specularContrast;
                        bw = bw < (specularContrast * specularCutOff) ? -1 : bw;
                        bw = Mathf.Clamp(bw, -1, 1);
                        bw *= 0.5f;
                        bw += 0.5f;
                        Color c = new Color(bw, bw, bw, 1);
                        pixels[x + y * inputTexture.width] = c;
                    }

                    // progress bar
                    progress += progressStep;
                    if (EditorUtility.DisplayCancelableProgressBar(appName, "Creating specular map..", progress))
                    {
                        Debug.Log(appName + ": Specular map creation cancelled by user (strange texture results will occur)");
                        EditorUtility.ClearProgressBar();
                        break;
                    }
                }
                EditorUtility.ClearProgressBar();

                // apply texture
                texSpecular.SetPixels(pixels);

                // save texture as png
                byte[] bytes3 = texSpecular.EncodeToPNG();
                File.WriteAllBytes(path.Substring(0, path.Length - Path.GetExtension(path).Length) + specularSuffix, bytes3);
                // cleanup texture
                UnityEngine.Object.DestroyImmediate(texSpecular);
            }

            if (normalGroupEnabled)
            {
                progress = 0;
                Color[] pixels = new Color[inputTexture.width * inputTexture.height];
                // sobel filter
                Texture2D texNormal = new Texture2D(inputTexture.width, inputTexture.height, TextureFormat.RGB24, false, false);
                Vector3 vScale = new Vector3(0.3333f, 0.3333f, 0.3333f);
                for (int y = 0; y < inputTexture.height; y++)
                {
                    for (int x = 0; x < inputTexture.width; x++)
                    {
                        Color tc = texSource.GetPixel(x - 1, y - 1);
                        Vector3 cSampleNegXNegY = new Vector3(tc.r, tc.g, tc.g);
                        tc = texSource.GetPixel(x, y - 1);
                        Vector3 cSampleZerXNegY = new Vector3(tc.r, tc.g, tc.g);
                        tc = texSource.GetPixel(x + 1, y - 1);
                        Vector3 cSamplePosXNegY = new Vector3(tc.r, tc.g, tc.g);
                        tc = texSource.GetPixel(x - 1, y);
                        Vector3 cSampleNegXZerY = new Vector3(tc.r, tc.g, tc.g);
                        tc = texSource.GetPixel(x + 1, y);
                        Vector3 cSamplePosXZerY = new Vector3(tc.r, tc.g, tc.g);
                        tc = texSource.GetPixel(x - 1, y + 1);
                        Vector3 cSampleNegXPosY = new Vector3(tc.r, tc.g, tc.g);
                        tc = texSource.GetPixel(x, y + 1);
                        Vector3 cSampleZerXPosY = new Vector3(tc.r, tc.g, tc.g);
                        tc = texSource.GetPixel(x + 1, y + 1);
                        Vector3 cSamplePosXPosY = new Vector3(tc.r, tc.g, tc.g);
                        float fSampleNegXNegY = Vector3.Dot(cSampleNegXNegY, vScale);
                        float fSampleZerXNegY = Vector3.Dot(cSampleZerXNegY, vScale);
                        float fSamplePosXNegY = Vector3.Dot(cSamplePosXNegY, vScale);
                        float fSampleNegXZerY = Vector3.Dot(cSampleNegXZerY, vScale);
                        float fSamplePosXZerY = Vector3.Dot(cSamplePosXZerY, vScale);
                        float fSampleNegXPosY = Vector3.Dot(cSampleNegXPosY, vScale);
                        float fSampleZerXPosY = Vector3.Dot(cSampleZerXPosY, vScale);
                        float fSamplePosXPosY = Vector3.Dot(cSamplePosXPosY, vScale);
                        float edgeX = (fSampleNegXNegY - fSamplePosXNegY) * 0.25f + (fSampleNegXZerY - fSamplePosXZerY) * 0.5f + (fSampleNegXPosY - fSamplePosXPosY) * 0.25f;
                        float edgeY = (fSampleNegXNegY - fSampleNegXPosY) * 0.25f + (fSampleZerXNegY - fSampleZerXPosY) * 0.5f + (fSamplePosXNegY - fSamplePosXPosY) * 0.25f;
                        Vector2 vEdge = new Vector2(edgeX, edgeY) * normalStrength;
                        Vector3 norm = new Vector3(vEdge.x, vEdge.y, 1.0f).normalized;
                        Color c = new Color(norm.x * 0.5f + 0.5f, norm.y * 0.5f + 0.5f, norm.z * 0.5f + 0.5f, 1);
                        pixels[x + y * inputTexture.width] = c;
                    }
                    // progress bar
                    progress += progressStep;
                    if (EditorUtility.DisplayCancelableProgressBar(appName, "Creating normal map..", progress))
                    {
                        Debug.Log(appName + ": Normal map creation cancelled by user (strange texture results will occur)");
                        EditorUtility.ClearProgressBar();
                        break;
                    }
                }

                // apply texture
                texNormal.SetPixels(pixels);

                // save texture as png
                byte[] bytes2 = texNormal.EncodeToPNG();
                File.WriteAllBytes(path.Substring(0, path.Length - Path.GetExtension(path).Length) + normalSuffix, bytes2);
                AssetDatabase.Refresh();
                TextureImporter importerNor = (TextureImporter)TextureImporter.GetAtPath(path.Substring(0, path.Length - Path.GetExtension(path).Length) + normalSuffix);
                importerNor.textureType = TextureImporterType.NormalMap;
                TextureImporter OrgImporter = (TextureImporter)TextureImporter.GetAtPath(path);
                OrgImporter.sRGBTexture = true;
                importerNor.SaveAndReimport();

                // remove progressbar
                EditorUtility.ClearProgressBar();

                // cleanup texture
                UnityEngine.Object.DestroyImmediate(texNormal);
            }

            // cleanup texture
            UnityEngine.Object.DestroyImmediate(texSource);

            // restore isReadable setting, if we had changed it
            if (setReadable)
            {
                textureImporter.isReadable = false;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                setReadable = false;
            }
            AssetDatabase.Refresh();

        }

        IEnumerator Post(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            postProgress = 1;
            postFlag = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                sDb64FromServer = request.downloadHandler.text;
                sDb64FromServer = sDb64FromServer.Remove(0, 9);
                sDb64FromServer = sDb64FromServer.Remove(sDb64FromServer.Length - 3);
                byte[] imageBytes = System.Convert.FromBase64String(sDb64FromServer);
                outputRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                outputRenderTexture.Create();
                outputTexture = new Texture2D(outputRenderTexture.width, outputRenderTexture.height);
                outputTexture.LoadImage(imageBytes);
                outputTexture.Apply();
                File.WriteAllBytes($"{_directoryPath}/{seed}.png", outputTexture.EncodeToPNG());
                AssetDatabase.Refresh();
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath($"{_directoryPath}/{seed}.png");
                request.Dispose();

            }
        }

        IEnumerator PostUpscale(string url, string bodyJsonString)
        {
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            postUpscaleProgress = 1;
            postUpscaleFlag = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                sDb64FromServer = request.downloadHandler.text;
                sDb64FromServer = sDb64FromServer.Remove(0, 9);
                sDb64FromServer = sDb64FromServer.Remove(sDb64FromServer.Length - 3);
                byte[] imageBytes = System.Convert.FromBase64String(sDb64FromServer);
                outputRenderTexture = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
                outputRenderTexture.Create();
                outputTexture = new Texture2D(outputRenderTexture.width, outputRenderTexture.height);
                outputTexture.LoadImage(imageBytes);
                outputTexture.Apply();
                File.WriteAllBytes($"{_directoryPath}/{seed}_x2.png", outputTexture.EncodeToPNG());
                AssetDatabase.Refresh();
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath($"{_directoryPath}/{seed}.png");
                request.Dispose();

            }
        }

        IEnumerator PostDepth(string url, string bodyJsonString)
        {
            string path = AssetDatabase.GetAssetPath(selectedTextures.ElementAt(0));
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(request.error);
            }
            else
            {
                resultFromServer = request.downloadHandler.text;
                resultFromServer = resultFromServer.Remove(0, 9);
                resultFromServer = resultFromServer.Remove(resultFromServer.Length - 3);
                byte[] imageBytes = System.Convert.FromBase64String(resultFromServer);
                outputRenderTexture = new RenderTexture(inputTexture.width, inputTexture.height, 16, RenderTextureFormat.ARGB32);
                outputRenderTexture.Create();
                outputTexture = new Texture2D(outputRenderTexture.width, outputRenderTexture.height);
                outputTexture.LoadImage(imageBytes);
                outputTexture.Apply();
                File.WriteAllBytes(path.Substring(0, path.Length - Path.GetExtension(path).Length) + depthSuffix, outputTexture.EncodeToPNG());
                AssetDatabase.Refresh();
                request.Dispose();
            }
        }

        private string Get(string url)
        {
            UnityWebRequest request = new UnityWebRequest(url, "GET");
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SendWebRequest();
            while (!request.isDone)
            {
                //Timeout Code
            }
            if (request.responseCode.ToString() != "200")
            {
                Debug.Log("Too many requests");
                return null;
            }
            else
            {
                return request.downloadHandler.text;
            }
        }

    }


} // namespace
