// UVMaskMakerWindow_en.cs
// English-only UI copy of the UV mask editor window.
// Shares analysis types (UVAnalysis/UVAnalyzer) defined in UVMaskMakerWindow.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dennoko.UVTools
{
    public class UVMaskMakerWindow_en : EditorWindow
    {
        // Persisted EditorPrefs keys
        private const string Pref_LastSaveDir = "Dennoko.UVTools.UVMaskMaker.LastSaveDir";
        private const string Pref_TextureSize = "Dennoko.UVTools.UVMaskMaker.TextureSize";
        private const string Pref_Hotkey = "Dennoko.UVTools.UVMaskMaker.Hotkey"; // legacy
        private const string Pref_AddHotkey = "Dennoko.UVTools.UVMaskMaker.AddHotkey";
        private const string Pref_RemoveHotkey = "Dennoko.UVTools.UVMaskMaker.RemoveHotkey";
        private const string Pref_PixelMargin = "Dennoko.UVTools.UVMaskMaker.PixelMargin";
        private const string Pref_ShowSelectedScene = "Dennoko.UVTools.UVMaskMaker.ShowSelectedScene";
        private const string Pref_SelectedSceneColor = "Dennoko.UVTools.UVMaskMaker.SelectedSceneColor";
        private const string Pref_PreviewFillSelectedColor = "Dennoko.UVTools.UVMaskMaker.PreviewFillSelectedColor";
        private const string Pref_OverlayOnTop = "Dennoko.UVTools.UVMaskMaker.OverlayOnTop";
        private const string Pref_OverlayDepthOffset = "Dennoko.UVTools.UVMaskMaker.OverlayDepthOffset";
        private const string Pref_OverlaySeamThickness = "Dennoko.UVTools.UVMaskMaker.OverlaySeamThickness";
        private const string Pref_UseBakedMesh = "Dennoko.UVTools.UVMaskMaker.UseBakedMesh";
        private const string Pref_PreviewOverlayBaseTex = "Dennoko.UVTools.UVMaskMaker.PreviewOverlayBaseTex";
        private const string Pref_PreviewOverlayAlpha = "Dennoko.UVTools.UVMaskMaker.PreviewOverlayAlpha";
        private const string Pref_ColorOptionsFoldout = "Dennoko.UVTools.UVMaskMaker.ColorOptionsFoldout";
        private const string Pref_ModeToggleHotkey = "Dennoko.UVTools.UVMaskMaker.ModeToggleHotkey";
        private const string Pref_SeamColor = "Dennoko.UVTools.UVMaskMaker.SeamColor";
        private const string Pref_AdvancedOptionsFoldout = "Dennoko.UVTools.UVMaskMaker.AdvancedOptionsFoldout";
    private const string Pref_ChannelWriteFoldout = "Dennoko.UVTools.UVMaskMaker.ChannelWriteFoldout";
    // Channel-wise export prefs
    private const string Pref_ChannelWrite = "Dennoko.UVTools.UVMaskMaker.ChannelWrite";
    private const string Pref_ChannelWrite_R = "Dennoko.UVTools.UVMaskMaker.ChannelWrite.R";
    private const string Pref_ChannelWrite_G = "Dennoko.UVTools.UVMaskMaker.ChannelWrite.G";
    private const string Pref_ChannelWrite_B = "Dennoko.UVTools.UVMaskMaker.ChannelWrite.B";
    private const string Pref_ChannelWrite_A = "Dennoko.UVTools.UVMaskMaker.ChannelWrite.A";
    private const string Pref_BasePNGAssetPath = "Dennoko.UVTools.UVMaskMaker.BasePNG";
    private const string Pref_BaseVCMeshAssetPath = "Dennoko.UVTools.UVMaskMaker.BaseVCMesh";

        // Log file paths
        private static string LogDir => Path.Combine(Application.dataPath, "../Logs/UVMaskMaker");
        private static string LogPath => Path.Combine(LogDir, "UVMaskMaker.log");

        // Target selection and mesh refs
        private GameObject _targetGO;
        private Renderer _targetRenderer;
        private Mesh _targetMesh;
        private Transform _targetTransform;

        // UI state
        private Vector2 _scrollPos;
        private int _textureSize = 512;
    private string _outputDir = "Assets/GeneratedMasks";
        private string _fileName = "uv_mask";
        private int _pixelMargin = 2;
        private bool _addMode = true;
        private KeyCode _modeToggleHotkey = KeyCode.R;

        private bool _showSelectedInScene = true; // always true now
        private Color _selectedSceneColor = new Color(0f, 1f, 1f, 1f); // cyan
        private Color _seamColor = new Color(1f, 0.15f, 0.15f, 1f);
        private Color _previewFillSelectedColor = Color.black;
        private bool _overlayOnTop = false; // draw overlays on top (zTest Always)
        private float _overlayDepthOffset = 0.0f; // meters
        private float _overlaySeamThickness = 2.5f; // pixels
        private bool _showColorOptionsFoldout = false;
        private bool _showAdvancedOptionsFoldout = false;
    private bool _showChannelWriteFoldout = false;
    // Channel-wise export fields
    private bool _channelWrite = false;
    private Texture2D _basePNG;
    private Mesh _baseVCMesh;
    private bool _cwR = true, _cwG = false, _cwB = false, _cwA = false;

        // Depth offset throttling (meters)
        private float _pendingOverlayDepthOffset = 0f;
        private double _nextDepthCommitTime = 0;
        private const double DepthCommitIntervalSec = 0.08; // seconds

        // Preview label-map cache
        private int[] _labelMap;
        private int _labelMapSize = 0;
        private bool _labelMapDirty = true;

        // Overlay geometry cache
        private Matrix4x4 _lastLocalToWorld;
        private bool _overlayCacheValid = false;
        private Vector3[] _worldPosBase;
        private Vector3[] _worldNormal;

        // Computed data
        private UVAnalysis _analysis;
        private HashSet<int> _selectedIslands = new HashSet<int>();
        private Texture2D _previewTex;
        private bool _previewDirty = true;
        private Texture2D _previewOverlayTex;
        private bool _previewOverlayBaseTex = false;
        private float _previewOverlayAlpha = 0.6f;

        // Picking helpers
        private MeshCollider _tempCollider;
        private GameObject _tempColliderGO;

        // Baked mesh
        private Mesh _bakedMesh;
        private bool _useBakedMesh = false;

        // Colors
        private static readonly Color IslandFillSelected = Color.black;
        private static readonly Color IslandFillUnselected = Color.white;
        private static readonly Color UVFrame = new Color(0.25f, 0.25f, 0.25f, 1);

        [MenuItem("Tools/UV Mask Maker (EN)")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<UVMaskMakerWindow_en>();
            wnd.titleContent = new GUIContent("UV Mask Maker (EN)");
            wnd.minSize = new Vector2(480, 520);
            wnd.Show();
        }

        private void OnEnable()
        {
            try { Directory.CreateDirectory(LogDir); Log($"[OnEnable] Window opened at {DateTime.Now:yyyy-MM-dd HH:mm:ss}"); } catch { }

            _textureSize = EditorPrefs.GetInt(Pref_TextureSize, 512);
            _outputDir = EditorPrefs.GetString(Pref_LastSaveDir, _outputDir);
            _modeToggleHotkey = (KeyCode)EditorPrefs.GetInt(Pref_ModeToggleHotkey, (int)KeyCode.R);
            _pixelMargin = EditorPrefs.GetInt(Pref_PixelMargin, 2);
            _showSelectedInScene = true;
            var colorHex = EditorPrefs.GetString(Pref_SelectedSceneColor, ColorUtility.ToHtmlStringRGBA(_selectedSceneColor));
            if (ColorUtility.TryParseHtmlString("#" + colorHex, out var parsedCol)) _selectedSceneColor = parsedCol;
            var seamHex = EditorPrefs.GetString(Pref_SeamColor, ColorUtility.ToHtmlStringRGBA(_seamColor));
            if (ColorUtility.TryParseHtmlString("#" + seamHex, out var parsedSeam)) _seamColor = parsedSeam;
            var pfillHex = EditorPrefs.GetString(Pref_PreviewFillSelectedColor, ColorUtility.ToHtmlStringRGBA(_previewFillSelectedColor));
            if (ColorUtility.TryParseHtmlString("#" + pfillHex, out var parsedFill)) _previewFillSelectedColor = parsedFill;
            _overlayOnTop = EditorPrefs.GetBool(Pref_OverlayOnTop, false);
            _overlayDepthOffset = EditorPrefs.GetFloat(Pref_OverlayDepthOffset, 0f);
            _overlaySeamThickness = EditorPrefs.GetFloat(Pref_OverlaySeamThickness, 2.5f);
            _useBakedMesh = EditorPrefs.GetBool(Pref_UseBakedMesh, false);
            _previewOverlayBaseTex = EditorPrefs.GetBool(Pref_PreviewOverlayBaseTex, false);
            _previewOverlayAlpha = EditorPrefs.GetFloat(Pref_PreviewOverlayAlpha, 0.6f);
            _showColorOptionsFoldout = EditorPrefs.GetBool(Pref_ColorOptionsFoldout, false);
            _showAdvancedOptionsFoldout = EditorPrefs.GetBool(Pref_AdvancedOptionsFoldout, false);
            _showChannelWriteFoldout = EditorPrefs.GetBool(Pref_ChannelWriteFoldout, false);
            _pendingOverlayDepthOffset = _overlayDepthOffset;
            // Channel-wise prefs
            _channelWrite = EditorPrefs.GetBool(Pref_ChannelWrite, false);
            _cwR = EditorPrefs.GetBool(Pref_ChannelWrite_R, true);
            _cwG = EditorPrefs.GetBool(Pref_ChannelWrite_G, false);
            _cwB = EditorPrefs.GetBool(Pref_ChannelWrite_B, false);
            _cwA = EditorPrefs.GetBool(Pref_ChannelWrite_A, false);
            var basePngPath = EditorPrefs.GetString(Pref_BasePNGAssetPath, string.Empty);
            if (!string.IsNullOrEmpty(basePngPath)) _basePNG = AssetDatabase.LoadAssetAtPath<Texture2D>(basePngPath);
            var baseVCPath = EditorPrefs.GetString(Pref_BaseVCMeshAssetPath, string.Empty);
            if (!string.IsNullOrEmpty(baseVCPath)) _baseVCMesh = AssetDatabase.LoadAssetAtPath<Mesh>(baseVCPath);

            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            CleanupTempCollider();
            if (_bakedMesh != null) { try { DestroyImmediate(_bakedMesh); } catch { } _bakedMesh = null; }
            if (_previewTex != null) { DestroyImmediate(_previewTex); _previewTex = null; }
            if (_previewOverlayTex != null) { DestroyImmediate(_previewOverlayTex); _previewOverlayTex = null; }
            Log("[OnDisable] Window closed");
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state) { }
        private void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path) { CleanupTempCollider(); }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target Model", EditorStyles.boldLabel);

            var dropRect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drag a GameObject with MeshRenderer/SkinnedMeshRenderer here", EditorStyles.helpBox);
            HandleDragAndDrop(dropRect);

            using (new EditorGUILayout.HorizontalScope())
            {
                var newObj = EditorGUILayout.ObjectField(new GUIContent("Object", "Target GameObject (contains MeshRenderer or SkinnedMeshRenderer)."), _targetGO, typeof(GameObject), true) as GameObject;
                if (newObj != _targetGO) { SetTarget(newObj); }
                GUI.enabled = _targetGO != null;
                if (GUILayout.Button(new GUIContent("Clear", "Clear the current target settings."), GUILayout.Width(60))) { SetTarget(null); }
                GUI.enabled = true;
            }

            if (_targetRenderer is SkinnedMeshRenderer)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool useBaked = EditorGUILayout.ToggleLeft(new GUIContent("Use Baked Mesh", "Use baked mesh to improve alignment for display/picking."), _useBakedMesh, GUILayout.Width(140));
                    if (useBaked != _useBakedMesh)
                    {
                        _useBakedMesh = useBaked; EditorPrefs.SetBool(Pref_UseBakedMesh, _useBakedMesh); _overlayCacheValid = false;
                        if (_tempCollider != null) { _tempCollider.sharedMesh = (_useBakedMesh && _bakedMesh != null) ? _bakedMesh : _targetMesh; }
                        SceneView.RepaintAll();
                    }
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Mode", "Behavior when clicking. Add adds islands, Remove excludes."), GUILayout.Width(50));
                int toolbar = GUILayout.Toolbar(_addMode ? 0 : 1, new[] { new GUIContent("Add", "Add the clicked UV island to selection."), new GUIContent("Remove", "Remove the clicked UV island from selection.") });
                _addMode = toolbar == 0;
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(new GUIContent("Toggle Hotkey", "Press to toggle Add/Remove mode."), GUILayout.Width(95));
                var toggleStr = EditorGUILayout.TextField(_modeToggleHotkey.ToString(), GUILayout.Width(60));
                if (Enum.TryParse<KeyCode>(toggleStr, out var toggleParsed) && toggleParsed != _modeToggleHotkey) { _modeToggleHotkey = toggleParsed; EditorPrefs.SetInt(Pref_ModeToggleHotkey, (int)_modeToggleHotkey); }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene Overlay", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool onTop = EditorGUILayout.ToggleLeft(new GUIContent("Draw On Top (X-Ray)", "Always draw wire/seams on top (disable ZTest)."), _overlayOnTop);
                if (onTop != _overlayOnTop) { _overlayOnTop = onTop; EditorPrefs.SetBool(Pref_OverlayOnTop, _overlayOnTop); SceneView.RepaintAll(); }
                GUILayout.FlexibleSpace();
            }

            _showAdvancedOptionsFoldout = EditorGUILayout.Foldout(_showAdvancedOptionsFoldout, new GUIContent("Advanced Options", "Open/close advanced settings."), true);
            if (_showAdvancedOptionsFoldout)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    float th = EditorGUILayout.Slider(new GUIContent("Thickness", "Line thickness in Scene view."), _overlaySeamThickness, 1f, 8f);
                    if (!Mathf.Approximately(th, _overlaySeamThickness)) { _overlaySeamThickness = th; EditorPrefs.SetFloat(Pref_OverlaySeamThickness, _overlaySeamThickness); SceneView.RepaintAll(); }

                    float pendingMm = _pendingOverlayDepthOffset * 1000f;
                    float newPendingMm = EditorGUILayout.Slider(new GUIContent("Depth Offset (mm)", "Offset wire/seams along normals to avoid z-fighting (unit: mm)."), pendingMm, 0f, 20f);
                    if (!Mathf.Approximately(newPendingMm, pendingMm)) { _pendingOverlayDepthOffset = Mathf.Clamp(newPendingMm, 0f, 20f) / 1000f; }

                    double now = EditorApplication.timeSinceStartup;
                    bool mouseUp = Event.current.type == EventType.MouseUp;
                    bool timeToCommit = now >= _nextDepthCommitTime;
                    if ((timeToCommit || mouseUp) && !Mathf.Approximately(_pendingOverlayDepthOffset, _overlayDepthOffset))
                    {
                        _overlayDepthOffset = _pendingOverlayDepthOffset; EditorPrefs.SetFloat(Pref_OverlayDepthOffset, _overlayDepthOffset); SceneView.RepaintAll(); _nextDepthCommitTime = now + DepthCommitIntervalSec;
                    }
                }
            }
            EditorPrefs.SetBool(Pref_AdvancedOptionsFoldout, _showAdvancedOptionsFoldout);

            _showColorOptionsFoldout = EditorGUILayout.Foldout(_showColorOptionsFoldout, new GUIContent("Color Options", "Open/close color settings."), true);
            if (_showColorOptionsFoldout)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    var selCol = EditorGUILayout.ColorField(new GUIContent("Selected Islands", "Wire color of selected islands in Scene view."), _selectedSceneColor);
                    if (selCol != _selectedSceneColor) { _selectedSceneColor = selCol; EditorPrefs.SetString(Pref_SelectedSceneColor, ColorUtility.ToHtmlStringRGBA(_selectedSceneColor)); SceneView.RepaintAll(); }

                    var seamCol = EditorGUILayout.ColorField(new GUIContent("Seam Color", "Seam line color in Scene view."), _seamColor);
                    if (seamCol != _seamColor) { _seamColor = seamCol; EditorPrefs.SetString(Pref_SeamColor, ColorUtility.ToHtmlStringRGBA(_seamColor)); SceneView.RepaintAll(); }

                    var pfill = EditorGUILayout.ColorField(new GUIContent("Preview Fill Color", "Fill color for selected area in preview (export is still B/W)."), _previewFillSelectedColor);
                    if (pfill != _previewFillSelectedColor) { _previewFillSelectedColor = pfill; EditorPrefs.SetString(Pref_PreviewFillSelectedColor, ColorUtility.ToHtmlStringRGBA(_previewFillSelectedColor)); _previewDirty = true; Repaint(); }

                    float a = EditorGUILayout.Slider(new GUIContent("Overlay Alpha", "Opacity of the overlay when drawn over base texture."), _previewOverlayAlpha, 0f, 1f);
                    if (!Mathf.Approximately(a, _previewOverlayAlpha)) { _previewOverlayAlpha = a; EditorPrefs.SetFloat(Pref_PreviewOverlayAlpha, _previewOverlayAlpha); _previewDirty = true; Repaint(); }
                }
            }
            EditorPrefs.SetBool(Pref_ColorOptionsFoldout, _showColorOptionsFoldout);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Analyze UVs", "Analyze UV islands and seams (UV borders)."), GUILayout.Height(24))) { AnalyzeTargetMesh(); }
                if (GUILayout.Button(new GUIContent("Invert", "Select unselected and deselect selected islands."), GUILayout.Height(24))) { InvertSelection(); }
                if (GUILayout.Button(new GUIContent("Select All", "Select all UV islands."), GUILayout.Height(24))) { SelectAll(); }
                if (GUILayout.Button(new GUIContent("Clear", "Clear selection."), GUILayout.Height(24))) { ClearSelection(); }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                var hasBase = GetBaseTexture() != null;
                using (new EditorGUI.DisabledScope(!hasBase))
                {
                    bool overlay = EditorGUILayout.ToggleLeft(new GUIContent("Overlay base texture in preview", "Draw semi-transparent selection overlay on the base texture in preview."), _previewOverlayBaseTex);
                    if (overlay != _previewOverlayBaseTex) { _previewOverlayBaseTex = overlay; EditorPrefs.SetBool(Pref_PreviewOverlayBaseTex, _previewOverlayBaseTex); Repaint(); }
                }
                if (!hasBase) EditorGUILayout.LabelField("(No base texture)", GUILayout.Width(140));
            }
            DrawUVPreview();

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                int newMargin = EditorGUILayout.IntSlider(new GUIContent("Pixel Margin", "Dilate black region by pixels to prevent bleeding at seams."), _pixelMargin, 0, 16);
                if (newMargin != _pixelMargin) { _pixelMargin = newMargin; EditorPrefs.SetInt(Pref_PixelMargin, _pixelMargin); _previewDirty = true; }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Size", "Preview/export texture resolution."), GUILayout.Width(100));
                int newSize = EditorGUILayout.IntPopup(_textureSize, new[] { "512", "1024", "2048", "4096" }, new[] { 512, 1024, 2048, 4096 }, GUILayout.Width(100));
                if (newSize != _textureSize) { _textureSize = newSize; _previewDirty = true; EditorPrefs.SetInt(Pref_TextureSize, _textureSize); }

                _fileName = EditorGUILayout.TextField(new GUIContent("File Name", "Output PNG file name (extension is added automatically)."), _fileName);
            }
            // Channel-wise export (foldable)
            _showChannelWriteFoldout = EditorGUILayout.Foldout(_showChannelWriteFoldout, new GUIContent("Channel-wise write", "Write to selected RGBA channels; supports PNG and vertex colors."), true);
            if (_showChannelWriteFoldout)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    bool ch = EditorGUILayout.ToggleLeft(new GUIContent("Enabled", "Enable channel-wise writing."), _channelWrite);
                    if (ch != _channelWrite) { _channelWrite = ch; EditorPrefs.SetBool(Pref_ChannelWrite, _channelWrite); }
                    using (new EditorGUI.DisabledScope(!_channelWrite))
                    {
                        var newBase = EditorGUILayout.ObjectField(new GUIContent("Base PNG", "Target PNG to overwrite. If empty, save as a new PNG."), _basePNG, typeof(Texture2D), false) as Texture2D;
                        if (newBase != _basePNG)
                        {
                            _basePNG = newBase;
                            string path = _basePNG ? AssetDatabase.GetAssetPath(_basePNG) : string.Empty;
                            EditorPrefs.SetString(Pref_BasePNGAssetPath, path);
                        }
                        EditorGUILayout.LabelField("Channels to write");
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            bool r = EditorGUILayout.ToggleLeft(new GUIContent("R", "Write to Red channel"), _cwR, GUILayout.Width(40));
                            bool g = EditorGUILayout.ToggleLeft(new GUIContent("G", "Write to Green channel"), _cwG, GUILayout.Width(40));
                            bool b = EditorGUILayout.ToggleLeft(new GUIContent("B", "Write to Blue channel"), _cwB, GUILayout.Width(40));
                            bool a = EditorGUILayout.ToggleLeft(new GUIContent("A", "Write to Alpha channel"), _cwA, GUILayout.Width(40));
                            if (r != _cwR) { _cwR = r; EditorPrefs.SetBool(Pref_ChannelWrite_R, _cwR); }
                            if (g != _cwG) { _cwG = g; EditorPrefs.SetBool(Pref_ChannelWrite_G, _cwG); }
                            if (b != _cwB) { _cwB = b; EditorPrefs.SetBool(Pref_ChannelWrite_B, _cwB); }
                            if (a != _cwA) { _cwA = a; EditorPrefs.SetBool(Pref_ChannelWrite_A, _cwA); }
                        }
                        // Base mesh for vertex color baking
                        var newBaseMesh = EditorGUILayout.ObjectField(new GUIContent("Base Mesh (Vertex Colors)", "Mesh used as a base for vertex color overwrite. Must match vertex count."), _baseVCMesh, typeof(Mesh), false) as Mesh;
                        if (newBaseMesh != _baseVCMesh)
                        {
                            _baseVCMesh = newBaseMesh;
                            string meshPath = _baseVCMesh ? AssetDatabase.GetAssetPath(_baseVCMesh) : string.Empty;
                            EditorPrefs.SetString(Pref_BaseVCMeshAssetPath, meshPath);
                        }
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("Bake to Vertex Colors", EditorStyles.boldLabel);
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(new GUIContent("Bake to Vertex Colors", "Bake the selection as vertex colors channel-wise and save a new mesh near the source."), GUILayout.Height(24)))
                            {
                                BakeMaskToVertexColors_ChannelWise();
                            }
                        }
                        _overwriteExistingVC = EditorGUILayout.ToggleLeft(new GUIContent("Overwrite if same name exists", "When enabled, overwrite mesh asset if the same name exists; otherwise save with a unique suffix."), _overwriteExistingVC);
                    }
                }
            }
            EditorPrefs.SetBool(Pref_ChannelWriteFoldout, _showChannelWriteFoldout);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(new GUIContent("Output Dir", "Output folder (under Assets)."), GUILayout.Width(90));
                EditorGUILayout.TextField(_outputDir);
                if (GUILayout.Button(new GUIContent("Browse", "Choose output folder (under project Assets)."), GUILayout.Width(90)))
                {
                    var selected = EditorUtility.OpenFolderPanel("Select Output Folder (under Assets)", _outputDir, "");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        if (selected.Contains("Assets"))
                        {
                            var projPath = Path.GetFullPath(Application.dataPath + "/..");
                            var rel = MakeProjectRelative(selected, projPath);
                            if (!string.IsNullOrEmpty(rel)) { _outputDir = rel.Replace('\\', '/'); EditorPrefs.SetString(Pref_LastSaveDir, _outputDir); }
                        }
                        else { EditorUtility.DisplayDialog("Invalid Folder", "Please select a folder under the project Assets directory.", "OK"); }
                    }
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUI.enabled = _analysis != null;
                if (GUILayout.Button(new GUIContent("Save PNG", "Save a PNG UV mask. If a base PNG is set, overwrite the selected channels."), GUILayout.Height(28), GUILayout.Width(140))) { SaveMaskPNG(); }
                GUI.enabled = true;
            }

            EditorGUILayout.HelpBox(
                $"How to use:\n1) Set a target and click 'Analyze UVs'.\n2) Left-click to modify selection following the current mode (Add/Remove).\n3) Press hotkey [{_modeToggleHotkey}] to toggle Add/Remove.\n4) Use Invert / Select All / Clear as needed.\n5) Configure Export and click Save PNG.",
                MessageType.Info);
            EditorGUILayout.EndScrollView();
        }

        private void HandleDragAndDrop(Rect dropRect)
        {
            var evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject go) { SetTarget(go); break; }
                    }
                }
                evt.Use();
            }
        }

        private void SetTarget(GameObject go)
        {
            _targetGO = go; _targetRenderer = null; _targetMesh = null; _targetTransform = null; _analysis = null; _selectedIslands.Clear(); _previewDirty = true; CleanupTempCollider();
            if (_bakedMesh != null) { try { DestroyImmediate(_bakedMesh); } catch { } _bakedMesh = null; }
            if (_targetGO == null) return;

            var smr = _targetGO.GetComponentInChildren<SkinnedMeshRenderer>();
            var mr = _targetGO.GetComponentInChildren<MeshRenderer>();
            if (smr != null) { _targetRenderer = smr; _targetMesh = smr.sharedMesh; _targetTransform = smr.transform; }
            else if (mr != null) { _targetRenderer = mr; var mf = mr.GetComponent<MeshFilter>(); _targetMesh = mf ? mf.sharedMesh : null; _targetTransform = mr.transform; }

            if (_targetMesh == null) { EditorUtility.DisplayDialog("No Mesh Found", "The selected object doesn't contain a Mesh.", "OK"); return; }

            Log($"[SetTarget] Target='{_targetGO.name}', Mesh='{_targetMesh.name}', V={_targetMesh.vertexCount}, T={_targetMesh.triangles.Length/3}");
            _fileName = _targetGO.name + "_mask";
            BakeCurrentPoseAuto();
            AnalyzeTargetMesh();
            EnsureTempCollider();
        }

        private void AnalyzeTargetMesh()
        {
            if (_targetMesh == null) { EditorUtility.DisplayDialog("No Target", "Please assign a target with a mesh.", "OK"); return; }
            try
            {
                _analysis = UVAnalyzer.Analyze(_targetMesh);
                _selectedIslands.Clear(); _previewDirty = true; _labelMapDirty = true; _overlayCacheValid = false; Repaint();
                BakeCurrentPoseAuto();
                Log($"[Analyze] Islands={_analysis.Islands.Count} BorderEdges={_analysis.BorderEdges.Count}");
            }
            catch (Exception ex) { Debug.LogError($"UV analysis failed: {ex.Message}\n{ex}"); Log($"[Analyze][Error] {ex}"); }
        }

        private void InvertSelection()
        {
            if (_analysis == null) return;
            var ns = new HashSet<int>();
            for (int i = 0; i < _analysis.Islands.Count; i++) if (!_selectedIslands.Contains(i)) ns.Add(i);
            _selectedIslands = ns; _previewDirty = true;
        }

        private void SelectAll()
        {
            if (_analysis == null) return;
            _selectedIslands = new HashSet<int>(Enumerable.Range(0, _analysis.Islands.Count));
            _previewDirty = true;
        }

        private void ClearSelection() { _selectedIslands.Clear(); _previewDirty = true; }

        private void DrawUVPreview()
        {
            var rect = GUILayoutUtility.GetAspectRect(1f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, UVFrame * 0.5f);
            if (_analysis == null) { GUI.Label(rect, "Analyze a mesh to preview UVs", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter }); return; }

            if (_previewTex == null || _previewTex.width != _textureSize)
            {
                if (_previewTex != null) DestroyImmediate(_previewTex);
                _previewTex = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "UVMaskPreview" };
                _previewDirty = true; _labelMapDirty = true;
            }
            if (_previewOverlayTex == null || _previewOverlayTex.width != _textureSize)
            {
                if (_previewOverlayTex != null) DestroyImmediate(_previewOverlayTex);
                _previewOverlayTex = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "UVMaskOverlay" };
                _previewDirty = true;
            }

            if (_previewDirty) { RegeneratePreviewTexture(_previewTex, _previewOverlayTex); _previewDirty = false; }

            if (Event.current.type == EventType.Repaint)
            {
                var pad = 6f; var texRect = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2, rect.height - pad * 2);
                var baseTex = _previewOverlayBaseTex ? GetBaseTexture() : null;
                if (baseTex != null) { GUI.DrawTexture(texRect, baseTex, ScaleMode.ScaleToFit, true); if (_previewOverlayTex != null) GUI.DrawTexture(texRect, _previewOverlayTex, ScaleMode.ScaleToFit, true); }
                else { GUI.DrawTexture(texRect, _previewTex, ScaleMode.ScaleToFit, true); }
                Handles.color = UVFrame; Handles.DrawLine(new Vector3(texRect.x, texRect.y), new Vector3(texRect.xMax, texRect.y)); Handles.DrawLine(new Vector3(texRect.xMax, texRect.y), new Vector3(texRect.xMax, texRect.yMax)); Handles.DrawLine(new Vector3(texRect.xMax, texRect.yMax), new Vector3(texRect.x, texRect.yMax)); Handles.DrawLine(new Vector3(texRect.x, texRect.yMax), new Vector3(texRect.x, texRect.y));
            }
        }

        private void RegeneratePreviewTexture(Texture2D tex, Texture2D overlay)
        {
            if (_labelMap == null || _labelMapSize != tex.width * tex.height || _labelMapDirty) { BuildLabelMap(tex.width, tex.height); }
            int count = tex.width * tex.height; var mask = new byte[count];
            for (int i = 0; i < count; i++) { int isl = _labelMap[i]; mask[i] = (byte)((isl >= 0 && _selectedIslands.Contains(isl)) ? 255 : 0); }
            if (_pixelMargin > 0) { DilateMaskBytes(mask, tex.width, tex.height, _pixelMargin); }
            var pixels32 = new Color32[count]; var selCol32 = (Color32)_previewFillSelectedColor; var unselCol32 = (Color32)IslandFillUnselected; for (int i = 0; i < count; i++) pixels32[i] = mask[i] != 0 ? selCol32 : unselCol32; tex.SetPixels32(pixels32); tex.Apply(false, false);
            if (overlay != null) { var ov = new Color32[count]; byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(_previewOverlayAlpha * 255f), 0, 255); var col = (Color32)_previewFillSelectedColor; col.a = a; var zero = new Color32(0, 0, 0, 0); for (int i = 0; i < count; i++) ov[i] = mask[i] != 0 ? col : zero; overlay.SetPixels32(ov); overlay.Apply(false, false); }
        }

        private Texture GetBaseTexture()
        {
            if (_targetRenderer == null) return null; var mats = _targetRenderer.sharedMaterials; if (mats == null) return null;
            foreach (var m in mats)
            {
                if (m == null) continue;
                if (m.HasProperty("_BaseMap")) { var t = m.GetTexture("_BaseMap"); if (t != null) return t; }
                if (m.HasProperty("_MainTex")) { var t = m.GetTexture("_MainTex"); if (t != null) return t; }
            }
            return null;
        }

        private static void RasterizeTriangleLabel(int W, int H, int[] labels, int islandIdx, Vector2 uv0, Vector2 uv1, Vector2 uv2)
        {
            Vector2 p0 = new Vector2(Mathf.Clamp01(uv0.x) * (W - 1), Mathf.Clamp01(uv0.y) * (H - 1));
            Vector2 p1 = new Vector2(Mathf.Clamp01(uv1.x) * (W - 1), Mathf.Clamp01(uv1.y) * (H - 1));
            Vector2 p2 = new Vector2(Mathf.Clamp01(uv2.x) * (W - 1), Mathf.Clamp01(uv2.y) * (H - 1));
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x))));
            int maxX = Mathf.Min(W - 1, Mathf.CeilToInt(Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x))));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y))));
            int maxY = Mathf.Min(H - 1, Mathf.CeilToInt(Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y))));
            float Edge(Vector2 a, Vector2 b, Vector2 c) => (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
            float area = Edge(p0, p1, p2); if (Mathf.Approximately(area, 0)) return;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float w0 = Edge(p1, p2, p); float w1 = Edge(p2, p0, p); float w2 = Edge(p0, p1, p);
                    bool inside = (w0 >= 0 && w1 >= 0 && w2 >= 0) || (w0 <= 0 && w1 <= 0 && w2 <= 0);
                    if (!inside) continue; int idx = y * W + x; labels[idx] = islandIdx;
                }
            }
        }

        private void BuildLabelMap(int width, int height)
        {
            _labelMap = UVMaskExport.BuildLabelMapTransient(_analysis, width, height);
            _labelMapSize = _labelMap.Length;
            _labelMapDirty = false;
        }

    private int[] BuildLabelMapTransient(int width, int height) => UVMaskExport.BuildLabelMapTransient(_analysis, width, height);

    private static void DilateMaskBytes(byte[] mask, int width, int height, int iterations) => UVMaskExport.DilateMaskBytes(mask, width, height, iterations);

        private void SaveMaskPNG()
        {
            if (_analysis == null) { EditorUtility.DisplayDialog("No Data", "Analyze a mesh first.", "OK"); return; }
            if (!AssetDatabase.IsValidFolder(_outputDir)) { UVMaskExport.EnsureAssetFolderPath(_outputDir); }
            string path = EditorUtility.SaveFilePanelInProject("Save UV Mask", _fileName, "png", "Choose location for the UV mask", _outputDir);
            if (string.IsNullOrEmpty(path)) return;

            int size = Mathf.Clamp(_textureSize, 8, 8192);
            var labels = BuildLabelMapTransient(size, size); var mask = new byte[size * size];
            for (int i = 0; i < mask.Length; i++) { int isl = labels[i]; mask[i] = (byte)((isl >= 0 && _selectedIslands.Contains(isl)) ? 255 : 0); }
            if (_pixelMargin > 0) { DilateMaskBytes(mask, size, size, _pixelMargin); }

            if (!_channelWrite)
            {
                var pixels32 = new Color32[mask.Length]; for (int i = 0; i < mask.Length; i++) pixels32[i] = mask[i] != 0 ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
                UVMaskExport.WritePngAtPath(path, pixels32, size); Log($"[Save] Wrote PNG {path} size={size}x{size}"); RevealSaved(path); return;
            }

            Color32[] basePixels;
            bool hasBase = _basePNG != null;
            if (hasBase)
            {
                string basePath = AssetDatabase.GetAssetPath(_basePNG);
                var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath);
                basePixels = UVMaskExport.LoadBasePixelsOrWhite(baseTex, size);
            }
            else { basePixels = Enumerable.Repeat(new Color32(255, 255, 255, 255), size * size).ToArray(); }

            // Compose mask into chosen channels
            for (int i = 0; i < basePixels.Length; i++)
            {
                bool selected = mask[i] != 0;
                byte v = (byte)(selected ? 0 : 255); // for RGB: selected=black
                var c = basePixels[i];
                if (hasBase)
                {
                    // Only overwrite selected pixels when a base PNG is provided
                    if (selected)
                    {
                        if (_cwR) c.r = v;
                        if (_cwG) c.g = v;
                        if (_cwB) c.b = v;
                        if (_cwA) c.a = 255;
                    }
                }
                else
                {
                    // No base PNG: write full channel from mask
                    if (_cwR) c.r = v;
                    if (_cwG) c.g = v;
                    if (_cwB) c.b = v;
                    if (_cwA) c.a = (byte)(mask[i]); // 255 selected, 0 unselected
                }
                basePixels[i] = c;
            }

            UVMaskExport.WritePngAtPath(path, basePixels, size); Log($"[Save] Wrote PNG (channel-wise) {path} size={size}x{size}"); RevealSaved(path);
        }

        private static void RevealSaved(string path)
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null) { ProjectWindowUtil.ShowCreatedAsset(obj); EditorGUIUtility.PingObject(obj); Selection.activeObject = obj; }
        }

        private void OnSceneGUI(SceneView sv)
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == _modeToggleHotkey)
            {
                _addMode = !_addMode; sv.ShowNotification(new GUIContent(_addMode ? "Mode: Add" : "Mode: Remove")); sv.Repaint(); Repaint(); e.Use();
            }

            if (_analysis != null && _targetTransform != null)
            {
                Handles.zTest = _overlayOnTop ? UnityEngine.Rendering.CompareFunction.Always : UnityEngine.Rendering.CompareFunction.LessEqual;
                EnsureWorldCache(); Handles.color = _seamColor;
                if (_analysis.BorderEdges != null)
                {
                    foreach (var be in _analysis.BorderEdges)
                    {
                        if (_worldPosBase == null || _worldNormal == null) break;
                        if ((uint)be.v0 >= _worldPosBase.Length || (uint)be.v1 >= _worldPosBase.Length) continue;
                        var a = _worldPosBase[be.v0] + _worldNormal[be.v0] * _overlayDepthOffset;
                        var b = _worldPosBase[be.v1] + _worldNormal[be.v1] * _overlayDepthOffset;
                        Handles.DrawAAPolyLine(_overlaySeamThickness, a, b);
                    }
                }
                if (_showSelectedInScene && _selectedIslands.Count > 0)
                {
                    Handles.color = _selectedSceneColor;
                    float thicknessBase = Mathf.Max(0.5f, _overlaySeamThickness);
                    float distForScale = 1f;
                    if (SceneView.lastActiveSceneView != null && SceneView.lastActiveSceneView.camera != null)
                    {
                        var cam = SceneView.lastActiveSceneView.camera;
                        var bounds = _targetRenderer != null ? _targetRenderer.bounds : new Bounds(_targetTransform.position, Vector3.one);
                        distForScale = Mathf.Max(0.1f, Vector3.Distance(cam.transform.position, bounds.center));
                    }
                    float thickness = Mathf.Clamp(thicknessBase / distForScale, 0.5f, thicknessBase);

                    foreach (var idx in _selectedIslands)
                    {
                        if (idx < 0 || idx >= _analysis.Islands.Count) continue;
                        var isl = _analysis.Islands[idx];
                        foreach (var tri in isl.Triangles)
                        {
                            if (_worldPosBase == null || _worldNormal == null) break;
                            if ((uint)tri.v0 >= _worldPosBase.Length || (uint)tri.v1 >= _worldPosBase.Length || (uint)tri.v2 >= _worldPosBase.Length) continue;
                            float baseOffset = _overlayDepthOffset;
                            var a = _worldPosBase[tri.v0] + _worldNormal[tri.v0] * baseOffset;
                            var b = _worldPosBase[tri.v1] + _worldNormal[tri.v1] * baseOffset;
                            var c = _worldPosBase[tri.v2] + _worldNormal[tri.v2] * baseOffset;
                            Handles.DrawAAPolyLine(thickness, a, b);
                            Handles.DrawAAPolyLine(thickness, b, c);
                            Handles.DrawAAPolyLine(thickness, c, a);
                        }
                    }
                }
            }

            if (_analysis != null && e.type == EventType.MouseDown && e.button == 0)
            {
                TryPickAndToggleIsland(sv, e.mousePosition, _addMode); e.Use();
            }
        }

        private bool _overwriteExistingVC = false;

        private void BakeMaskToVertexColors()
        {
            if (_targetMesh == null || _analysis == null) { EditorUtility.DisplayDialog("No Target", "Analyze a mesh first.", "OK"); return; }
            try
            {
                var colors = UVVertexColorBaker.BuildVertexColors(_analysis, _selectedIslands, _targetMesh.vertexCount);
                var colored = UVVertexColorBaker.CreateColoredMesh(_targetMesh, colors);
                var folder = UVVertexColorBaker.GetDefaultBakeFolderForMesh(_targetMesh);
                var nameNoExt = _targetMesh.name + "_WithVertexColors";
                var assetPath = UVVertexColorBaker.SaveMeshAsset(colored, folder, nameNoExt, _overwriteExistingVC);
                Log($"[BakeVC] Saved mesh with vertex colors: {assetPath}");
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (obj != null) { ProjectWindowUtil.ShowCreatedAsset(obj); EditorGUIUtility.PingObject(obj); Selection.activeObject = obj; }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Bake vertex colors failed: {ex.Message}\n{ex}");
                EditorUtility.DisplayDialog("Error", "Failed to bake vertex colors. Check Console for details.", "OK");
            }
        }

        private void BakeMaskToVertexColors_ChannelWise()
        {
            if (_targetMesh == null || _analysis == null) { EditorUtility.DisplayDialog("No Target", "Analyze a mesh first.", "OK"); return; }
            try
            {
                // Determine base colors: prefer user-specified base mesh if valid
                Color32[] baseColors = null;
                if (_baseVCMesh != null)
                {
                    if (_baseVCMesh.vertexCount != _targetMesh.vertexCount)
                    {
                        EditorUtility.DisplayDialog("Vertex count mismatch", "The base mesh vertex count doesn't match the target. Falling back to target's vertex colors.", "OK");
                        baseColors = _targetMesh.colors32;
                    }
                    else
                    {
                        baseColors = _baseVCMesh.colors32;
                    }
                }
                else
                {
                    baseColors = _targetMesh.colors32;
                }
                var colors = UVVertexColorBaker.BuildVertexColorsChannelWise(
                    _analysis, _selectedIslands, _targetMesh.vertexCount, baseColors, _cwR, _cwG, _cwB, _cwA);
                var colored = UVVertexColorBaker.CreateColoredMesh(_targetMesh, colors);
                var folder = UVVertexColorBaker.GetDefaultBakeFolderForMesh(_targetMesh);
                var nameNoExt = _targetMesh.name + "_WithVertexColors";
                var assetPath = UVVertexColorBaker.SaveMeshAsset(colored, folder, nameNoExt, _overwriteExistingVC);
                Log($"[BakeVC-CH] Saved mesh with vertex colors: {assetPath}");
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (obj != null) { ProjectWindowUtil.ShowCreatedAsset(obj); EditorGUIUtility.PingObject(obj); Selection.activeObject = obj; }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Bake vertex colors (channel-wise) failed: {ex.Message}\n{ex}");
                EditorUtility.DisplayDialog("Error", "Failed to bake vertex colors (channel-wise). Check Console for details.", "OK");
            }
        }

        private void EnsureWorldCache()
        {
            if (_targetTransform == null || _analysis == null) return;
            var l2w = _targetTransform.localToWorldMatrix; var mesh = (_useBakedMesh && _bakedMesh != null) ? _bakedMesh : null;
            bool needRebuild = !_overlayCacheValid || _worldPosBase == null || _worldNormal == null || _lastLocalToWorld != l2w;
            if (mesh != null) { if (_worldPosBase == null || _worldPosBase.Length != mesh.vertexCount) needRebuild = true; }
            else { if (_worldPosBase == null || _worldPosBase.Length != _analysis.Vertices.Count) needRebuild = true; }
            if (!needRebuild) return;

            int vCount = mesh != null ? mesh.vertexCount : _analysis.Vertices.Count;
            if (_worldPosBase == null || _worldPosBase.Length != vCount) _worldPosBase = new Vector3[vCount];
            if (_worldNormal == null || _worldNormal.Length != vCount) _worldNormal = new Vector3[vCount];

            if (mesh != null)
            {
                var verts = mesh.vertices; var norms = mesh.normals;
                for (int i = 0; i < vCount; i++)
                {
                    var lp = (i < verts.Length) ? verts[i] : Vector3.zero;
                    var ln = (norms != null && i < norms.Length) ? norms[i] : Vector3.up; if (ln.sqrMagnitude < 1e-8f) ln = Vector3.up;
                    _worldPosBase[i] = _targetTransform.TransformPoint(lp); _worldNormal[i] = _targetTransform.TransformDirection(ln).normalized;
                }
            }
            else
            {
                for (int i = 0; i < vCount; i++)
                {
                    var lp = _analysis.Vertices[i]; var ln = (i < _analysis.Normals.Count) ? _analysis.Normals[i] : Vector3.up; if (ln.sqrMagnitude < 1e-8f) ln = Vector3.up;
                    _worldPosBase[i] = _targetTransform.TransformPoint(lp); _worldNormal[i] = _targetTransform.TransformDirection(ln).normalized;
                }
            }

            _lastLocalToWorld = l2w; _overlayCacheValid = true;
        }

        private void TryPickAndToggleIsland(SceneView sv, Vector2 guiPos, bool add)
        {
            if (_targetGO == null || _targetMesh == null) return; EnsureTempCollider(); if (_tempCollider == null) return;
            Ray ray = HandleUtility.GUIPointToWorldRay(guiPos);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity))
            {
                if (hit.collider != _tempCollider) return; int triIndex = hit.triangleIndex;
                if (!_analysis.TriangleToIsland.TryGetValue(triIndex, out int islandIdx)) return;
                bool wasSelected = _selectedIslands.Contains(islandIdx);
                if (add) _selectedIslands.Add(islandIdx); else _selectedIslands.Remove(islandIdx);
                _previewDirty = true; Repaint(); sv.Repaint();
                Log($"[Pick] tri={triIndex} island={islandIdx} {( add ? "ADD" : "REMOVE")} (wasSelected={wasSelected})");
            }
        }

        private void EnsureTempCollider()
        {
            if (_targetGO == null || _targetMesh == null) { CleanupTempCollider(); return; }
            if (_tempCollider != null) return;
            _tempColliderGO = new GameObject("__UVMaskPickerCollider__"); _tempColliderGO.hideFlags = HideFlags.HideAndDontSave;
            _tempColliderGO.transform.SetPositionAndRotation(_targetTransform.position, _targetTransform.rotation);
            _tempColliderGO.transform.localScale = _targetTransform.lossyScale;
            _tempCollider = _tempColliderGO.AddComponent<MeshCollider>();
            _tempCollider.sharedMesh = (_useBakedMesh && _bakedMesh != null) ? _bakedMesh : _targetMesh; _tempCollider.convex = false; Log("[Collider] Created temporary MeshCollider for picking");
        }

        private void BakeCurrentPose()
        {
            if (!(_targetRenderer is SkinnedMeshRenderer smr)) { EditorUtility.DisplayDialog("Bake", "Bake is only available for SkinnedMeshRenderer.", "OK"); return; }
            if (_bakedMesh == null) _bakedMesh = new Mesh { name = $"{_targetMesh?.name}_Baked" }; else _bakedMesh.Clear();
            try
            {
                smr.BakeMesh(_bakedMesh); _overlayCacheValid = false; if (_tempCollider != null) _tempCollider.sharedMesh = _useBakedMesh ? _bakedMesh : _targetMesh; SceneView.RepaintAll(); Log($"[Bake] Baked pose Mesh verts={_bakedMesh.vertexCount} tris={_bakedMesh.triangles.Length/3}");
            }
            catch (Exception ex) { Debug.LogError($"Bake failed: {ex.Message}\n{ex}"); Log($"[Bake][Error] {ex}"); }
        }

        private void BakeCurrentPoseAuto()
        {
            if (!(_targetRenderer is SkinnedMeshRenderer smr)) return;
            if (_bakedMesh == null) _bakedMesh = new Mesh { name = $"{_targetMesh?.name}_Baked" }; else _bakedMesh.Clear();
            try
            {
                smr.BakeMesh(_bakedMesh); _overlayCacheValid = false; if (_tempCollider != null) _tempCollider.sharedMesh = _useBakedMesh ? _bakedMesh : _targetMesh;
            }
            catch { }
        }

        private void CleanupTempCollider()
        {
            if (_tempColliderGO != null)
            {
                try { DestroyImmediate(_tempColliderGO); Log("[Collider] Cleaned up temporary MeshCollider"); } catch { }
                _tempColliderGO = null; _tempCollider = null;
            }
        }

        private static string MakeProjectRelative(string absPath, string projectRoot)
        {
            try
            {
                var full = Path.GetFullPath(absPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var root = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return null;
                var rel = full.Substring(root.Length + 1); return rel;
            }
            catch { return null; }
        }

        private static void EnsureAssetFolderPath(string assetFolderPath)
        {
            if (string.IsNullOrEmpty(assetFolderPath)) return; assetFolderPath = assetFolderPath.Replace('\\', '/'); if (!assetFolderPath.StartsWith("Assets")) return;
            var parts = assetFolderPath.Split('/'); string current = parts[0];
            for (int i = 1; i < parts.Length; i++) { string next = current + "/" + parts[i]; if (!AssetDatabase.IsValidFolder(next)) { AssetDatabase.CreateFolder(current, parts[i]); } current = next; }
        }

        private static void Log(string msg)
        {
            try { Directory.CreateDirectory(LogDir); File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss} {msg}\n"); } catch { }
            Debug.Log($"[UVMaskMaker] {msg}");
        }
    }
}
