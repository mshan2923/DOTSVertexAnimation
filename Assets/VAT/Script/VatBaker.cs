using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class VATBaker : EditorWindow
{
    private GameObject _targetObject;
    private List<AnimationClip> _clips = new List<AnimationClip>();
    private SkinnedMeshRenderer _selectedSmr;
    private string[] _smrNames;
    private int _smrIndex;
    private int _fps = 30;
    private bool _bakeNormals = true;
    private string _savePath = "Assets/VAT";
    private string _statusMessage = "";
    private bool _isBaking = false;
    private Vector2 _scrollPos;

    [MenuItem("Tools/VAT Baker (Atlas)")]
    public static void OpenWindow() => GetWindow<VATBaker>("VAT Baker").minSize = new Vector2(400, 450);

    private void OnGUI()
    {
        GUILayout.Label("VAT Atlas Baker", EditorStyles.boldLabel);
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        var prevTarget = _targetObject;
        _targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", _targetObject, typeof(GameObject), true);

        if (_targetObject != prevTarget)
        {
            _smrIndex = 0;
            if (_targetObject != null)
            {
                var smrs = _targetObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                _smrNames = System.Array.ConvertAll(smrs, s => s.name);
            }
            else _smrNames = null;
        }

        if (_smrNames != null && _smrNames.Length > 0)
        {
            _smrIndex = EditorGUILayout.Popup("Skinned Mesh", _smrIndex, _smrNames);
            _selectedSmr = _targetObject.GetComponentsInChildren<SkinnedMeshRenderer>()[_smrIndex];
        }

        EditorGUILayout.Space(10);
        GUILayout.Label("Animation Clips", EditorStyles.miniBoldLabel);
        for (int i = 0; i < _clips.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _clips[i] = (AnimationClip)EditorGUILayout.ObjectField($"Clip {i}", _clips[i], typeof(AnimationClip), false);
            if (GUILayout.Button("X", GUILayout.Width(20))) { _clips.RemoveAt(i); break; }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add Animation Clip")) _clips.Add(null);

        EditorGUILayout.Space(10);
        _fps = EditorGUILayout.IntSlider("FPS", _fps, 1, 60);
        _bakeNormals = EditorGUILayout.Toggle("Bake Normals", _bakeNormals);
        _savePath = EditorGUILayout.TextField("Save Path", _savePath);
        EditorGUILayout.Space(20);

        bool canBake = _targetObject != null && _selectedSmr != null && _clips.Count > 0 && !_isBaking;
        foreach (var c in _clips) if (c == null) canBake = false;

        GUI.enabled = canBake;
        if (GUILayout.Button(_isBaking ? "Baking..." : "Bake VAT Atlas", GUILayout.Height(40))) Bake();
        GUI.enabled = true;

        if (!string.IsNullOrEmpty(_statusMessage))
            EditorGUILayout.HelpBox(_statusMessage, MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void Bake()
    {
        _isBaking = true;
        _statusMessage = "베이킹 준비 중...";
        Repaint();

        // 트랜스폼 백업 + 원점 정렬
        var t = _targetObject.transform;
        Vector3 oPos = t.position;
        Quaternion oRot = t.rotation;
        Vector3 oScl = t.localScale;
        t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        t.localScale = Vector3.one;

        try
        {
            var smr = _selectedSmr;
            int vertCount = smr.sharedMesh.vertexCount;
            int animCount = _clips.Count;

            // 프레임 수 / 아틀라스 오프셋 계산
            int[] clipFrames = new int[animCount];
            int[] startFrames = new int[animCount];
            int totalFrames = 0;
            for (int i = 0; i < animCount; i++)
            {
                clipFrames[i] = Mathf.Max(1, Mathf.RoundToInt(_clips[i].length * _fps));
                startFrames[i] = totalFrames;
                totalFrames += clipFrames[i];
            }

            if (vertCount > 16384 || totalFrames > 16384)
            {
                _statusMessage = $"? 텍스처 크기 초과 (v:{vertCount}, f:{totalFrames})";
                return;
            }

            var allPos = new Vector3[totalFrames * vertCount];
            var allNorms = new Vector3[totalFrames * vertCount];
            float posMin = float.MaxValue, posMax = float.MinValue;
            var bakedMesh = new Mesh();

            for (int i = 0; i < animCount; i++)
            {
                float dt = _clips[i].length / clipFrames[i];
                for (int f = 0; f < clipFrames[i]; f++)
                {
                    // ★ useScale = false : 로컬 스페이스 그대로 (뻥튀기 방지)
                    _clips[i].SampleAnimation(_targetObject, f * dt);
                    smr.BakeMesh(bakedMesh, false);

                    int af = startFrames[i] + f;
                    var verts = bakedMesh.vertices;
                    var norms = bakedMesh.normals;

                    for (int v = 0; v < vertCount; v++)
                    {
                        Vector3 p = verts[v];
                        allPos[af * vertCount + v] = p;
                        posMin = Mathf.Min(posMin, p.x, p.y, p.z);
                        posMax = Mathf.Max(posMax, p.x, p.y, p.z);

                        if (_bakeNormals)
                            allNorms[af * vertCount + v] = norms[v];
                    }
                }
            }

            if (Mathf.Approximately(posMin, posMax)) posMax = posMin + 0.001f;
            float range = posMax - posMin;
            string baseName = $"{_targetObject.name}_Atlas";

            // Position Texture
            var posTex = new Texture2D(vertCount, totalFrames, TextureFormat.RGBAHalf, false);
            posTex.filterMode = FilterMode.Bilinear;
            posTex.wrapMode = TextureWrapMode.Clamp;
            var posColors = new Color[allPos.Length];
            for (int i = 0; i < allPos.Length; i++)
                posColors[i] = new Color(
                    (allPos[i].x - posMin) / range,
                    (allPos[i].y - posMin) / range,
                    (allPos[i].z - posMin) / range, 1f);
            posTex.SetPixels(posColors);
            posTex.Apply();

            // Normal Texture
            Texture2D normTex = null;
            if (_bakeNormals)
            {
                normTex = new Texture2D(vertCount, totalFrames, TextureFormat.RGBAHalf, false);
                normTex.filterMode = FilterMode.Bilinear;
                normTex.wrapMode = TextureWrapMode.Clamp;
                var normColors = new Color[allNorms.Length];
                for (int i = 0; i < allNorms.Length; i++)
                    normColors[i] = new Color(
                        allNorms[i].x * 0.5f + 0.5f,
                        allNorms[i].y * 0.5f + 0.5f,
                        allNorms[i].z * 0.5f + 0.5f, 1f);
                normTex.SetPixels(normColors);
                normTex.Apply();
            }

            // Meta Texture (클립별 startFrame / frameCount / fps)
            var metaTex = new Texture2D(animCount, 1, TextureFormat.RGBAFloat, false);
            metaTex.filterMode = FilterMode.Point;
            metaTex.wrapMode = TextureWrapMode.Clamp;
            var metaColors = new Color[animCount];
            for (int i = 0; i < animCount; i++)
                metaColors[i] = new Color(startFrames[i], clipFrames[i], _fps, 1f);
            metaTex.SetPixels(metaColors);
            metaTex.Apply();

            // VAT 메시 (UV1 = VertexID)
            var vatMesh = Instantiate(smr.sharedMesh);
            vatMesh.normals = smr.sharedMesh.normals;
            vatMesh.tangents = smr.sharedMesh.tangents;
            var vIDs = new Vector2[vertCount];
            for (int v = 0; v < vertCount; v++) vIDs[v] = new Vector2(v, 0f);
            vatMesh.uv2 = vIDs;

            // 저장
            if (!Directory.Exists(_savePath)) Directory.CreateDirectory(_savePath);
            SaveTexture(posTex, Path.Combine(_savePath, $"{baseName}_pos.asset"));
            if (_bakeNormals)
                SaveTexture(normTex, Path.Combine(_savePath, $"{baseName}_norm.asset"));
            SaveTexture(metaTex, Path.Combine(_savePath, $"{baseName}_metaTex.asset"));

            string meshPath = Path.Combine(_savePath, $"{baseName}_mesh.asset");
            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingMesh != null) { EditorUtility.CopySerialized(vatMesh, existingMesh); AssetDatabase.SaveAssets(); }
            else AssetDatabase.CreateAsset(vatMesh, meshPath);

            // JSON 메타 (1회만!)
            SaveMeta(baseName, posMin, posMax, vertCount, totalFrames, animCount, startFrames, clipFrames);

            AssetDatabase.Refresh();
            _statusMessage = $"? 완료! posRange: {range:F4}  |  {vertCount}verts × {totalFrames}frames ({animCount}clips)";
        }
        catch (System.Exception e)
        {
            _statusMessage = $"? 오류: {e.Message}";
            Debug.LogException(e);
        }
        finally
        {
            t.SetPositionAndRotation(oPos, oRot);
            t.localScale = oScl;
            _isBaking = false;
            Repaint();
        }
    }

    private void SaveMeta(string baseName, float posMin, float posMax,
                          int vertCount, int totalFrames, int animCount,
                          int[] startFrames, int[] clipFrames)
    {
        var clips = new VATClipInfo[animCount];
        for (int i = 0; i < animCount; i++)
            clips[i] = new VATClipInfo
            {
                clipName = _clips[i].name,
                startFrame = startFrames[i],
                frameCount = clipFrames[i],
                clipLength = _clips[i].length
            };

        var meta = new VATAtlasMeta
        {
            atlasName = baseName,
            posMin = posMin,
            posMax = posMax,
            posRange = posMax - posMin,
            vertexCount = vertCount,
            totalFrames = totalFrames,
            fps = _fps,
            clips = clips
        };

        File.WriteAllText(
            Path.Combine(_savePath, $"{baseName}_meta.json"),
            JsonUtility.ToJson(meta, true));
    }

    private static void SaveTexture(Texture2D tex, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null) { EditorUtility.CopySerialized(tex, existing); AssetDatabase.SaveAssets(); }
        else AssetDatabase.CreateAsset(tex, path);
    }

    [System.Serializable]
    public class VATAtlasMeta
    {
        public string atlasName;
        public float posMin;
        public float posMax;
        public float posRange;
        public int vertexCount;
        public int totalFrames;
        public int fps;
        public VATClipInfo[] clips;
    }

    [System.Serializable]
    public class VATClipInfo
    {
        public string clipName;
        public int startFrame;
        public int frameCount;
        public float clipLength;
    }
}