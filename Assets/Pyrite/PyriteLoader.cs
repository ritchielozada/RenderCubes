﻿namespace Pyrite3D
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Extensions;
    using Microsoft.Xna.Framework;
    using Model;
    using RestSharp;
    using UnityEngine;
    using Debug = UnityEngine.Debug;

    public class PyriteLoader : MonoBehaviour
    {
        public string ModelVersion = "V2";
        private readonly Stopwatch _sw = Stopwatch.StartNew();

        private readonly Color[] _colorList =
        {
            Color.gray, Color.yellow, Color.cyan
        };

        private readonly Dictionary<string, byte[]> _eboCache = new Dictionary<string, byte[]>();
        private readonly Dictionary<string, Material[]> _materialCache = new Dictionary<string, Material[]>();

        private readonly Dictionary<string, List<MaterialData>> _materialDataCache =
            new Dictionary<string, List<MaterialData>>();

        private readonly Dictionary<string, string> _objCache = new Dictionary<string, string>();
        private readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();

        public GameObject CameraRig;
        private int _colorSelector;
        public int DetailLevel = 6;
        public bool EnableDebugLogs = false;
        public GameObject PlaceHolderCube;
        public string PyriteServer;
        public string SetName;
        public bool UseCameraDetection = false;
        public bool UseEbo = true;
        public bool UseUnlitShader = true;
        public bool UseWww = false;

        [Range(0, 100)] public int MaxConcurrentRequests = 8;

        private readonly HashSet<string> _activeRequests = new HashSet<string>();

        private readonly Queue<LoadCubeRequest> _loadCubeRequests = new Queue<LoadCubeRequest>();

        private IEnumerator StartRequest(string path)
        {
            if (MaxConcurrentRequests == 0)
            {
                // Not limiting requests
                yield break;
            }
            while (_activeRequests.Count >= MaxConcurrentRequests)
            {
                yield return null;
            }
            _activeRequests.Add(path);
        }

        private void EndRequest(string path)
        {
            if (MaxConcurrentRequests == 0)
            {
                // Not limiting requests
                return;
            }
            _activeRequests.Remove(path);
        }

        protected void DebugLog(string fmt, params object[] args)
        {
            if (EnableDebugLogs)
            {
                var content = string.Format(fmt, args);
                Debug.LogFormat("{0}: {1}", _sw.ElapsedMilliseconds, content);
            }
        }

        private void LogResponseError(IRestResponse response, string path = "")
        {
            if (response == null)
            {
                Debug.LogErrorFormat("Response is null [{0}]", path);
                return;
            }

            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                Debug.LogErrorFormat("Response.ErrorMessage: {0} [{1}]", response.ErrorMessage, path);
            }

            if (response.ErrorException != null)
            {
                Debug.LogErrorFormat("Response.ErrorException: {0} [{1}]", response.ErrorException, path);
            }
        }

        private void LogWwwError(WWW www, string path)
        {
            if (www == null)
            {
                Debug.LogErrorFormat("WWW is null [{0}]", path);
                return;
            }

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogErrorFormat("WWW.error: {0} [{1}]", www.error, path);
            }
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(SetName))
            {
                Debug.LogError("Must specify SetName");
                return;
            }

            if (string.IsNullOrEmpty(ModelVersion))
            {
                Debug.LogError("Must specify ModelVersion");
                return;
            }

            DebugLog("+Start()");
            StartCoroutine(Load());
            DebugLog("-Start()");
        }

        private void Update()
        {
            while (_loadCubeRequests.Count > 0)
            {
                var request = _loadCubeRequests.Dequeue();
                if (!request.Cancelled)
                {
                    StartCoroutine(ProcessLoadCubeRequest(request));
                }
            }
        }

        private PyriteCube CreateCubeFromCubeBounds(CubeBounds cubeBounds)
        {
            return new PyriteCube
            {
                X = (int) cubeBounds.BoundingBox.Min.x,
                Y = (int) cubeBounds.BoundingBox.Min.y,
                Z = (int) cubeBounds.BoundingBox.Min.z
            };
        }

        public IEnumerator Load()
        {
            DebugLog("+Load()");

            var pyriteQuery = new PyriteQuery(this, SetName, ModelVersion, PyriteServer);
            yield return StartCoroutine(pyriteQuery.LoadAll());
            DebugLog("CubeQuery complete.");

            var pyriteLevel =
                pyriteQuery.DetailLevels[DetailLevel];

            var allOctCubes = pyriteQuery.DetailLevels[DetailLevel].Octree.AllItems();

            foreach (var octCube in allOctCubes)
            {
                var pCube = CreateCubeFromCubeBounds(octCube);
                var x = pCube.X;
                var y = pCube.Y;
                var z = pCube.Z;
                var cubePos = pyriteLevel.GetWorldCoordinatesForCube(pCube);

                if (UseCameraDetection)
                {
                    // Move cube to the orientation we want also move it up since the model is around -600
                    var g =
                        (GameObject)
                            Instantiate(PlaceHolderCube, new Vector3(-cubePos.x, cubePos.z + 600, -cubePos.y),
                                Quaternion.identity);

                    g.transform.parent = gameObject.transform;
                    g.GetComponent<MeshRenderer>().material.color = _colorList[_colorSelector%_colorList.Length];
                    g.GetComponent<IsRendered>().SetCubePosition(x, y, z, DetailLevel, pyriteQuery, this);

                    g.transform.localScale = new Vector3(
                        pyriteLevel.WorldCubeScale.x,
                        pyriteLevel.WorldCubeScale.z,
                        pyriteLevel.WorldCubeScale.y);
                    _colorSelector++;
                }
                else
                {
                    var loadRequest = new LoadCubeRequest(x, y, z, DetailLevel, pyriteQuery, null);
                    EnqueueLoadCubeRequest(loadRequest);
                }
            }

            if (CameraRig != null)
            {
                DebugLog("Moving camera");
                // Hardcoding some values for now

                var min = new Vector3(pyriteLevel.ModelBoundsMin.x, pyriteLevel.ModelBoundsMin.y,
                    pyriteLevel.ModelBoundsMin.z);
                var max = new Vector3(pyriteLevel.ModelBoundsMax.x, pyriteLevel.ModelBoundsMax.y,
                    pyriteLevel.ModelBoundsMax.z);
                min += pyriteLevel.WorldCubeScale/2;
                max -= pyriteLevel.WorldCubeScale/2;
                var newCameraPosition = min + (max - min)/2.0f;
                newCameraPosition += new Vector3(0, 0, (max - min).z*1.4f);
                CameraRig.transform.position = newCameraPosition;

                CameraRig.transform.rotation = Quaternion.Euler(0, 180, 0);

                DebugLog("Done moving camera");
            }
            DebugLog("-Load()");
        }

        public IEnumerator AddUpgradedDetectorCubes(PyriteQuery pyriteQuery, int x, int y, int z, int lod,
            Action<IEnumerable<GameObject>> registerCreatedDetectorCubes)
        {
            var newLod = lod - 1;
            var createdDetectors = new List<GameObject>();
            var pyriteLevel = pyriteQuery.DetailLevels[newLod];

            var cubeFactor = pyriteQuery.GetNextCubeFactor(lod);
            var min = new Vector3(x*(int) cubeFactor.x + 0.5f, y*(int) cubeFactor.y + 0.5f, z*(int) cubeFactor.z + 0.5f);
            var max = new Vector3((x + 1)*(int) cubeFactor.x - 0.5f, (y + 1)*(int) cubeFactor.y - 0.5f,
                (z + 1)*(int) cubeFactor.z - 0.5f);
            var intersections =
                pyriteQuery.DetailLevels[newLod].Octree.AllIntersections(new BoundingBox {Min = min, Max = max});
            foreach (var i in intersections)
            {
                var newCube = CreateCubeFromCubeBounds(i.Object);
                var cubePos = pyriteLevel.GetWorldCoordinatesForCube(newCube);
                var g =
                    (GameObject)
                        Instantiate(PlaceHolderCube, new Vector3(-cubePos.x, cubePos.z + 600, -cubePos.y),
                            Quaternion.identity);

                g.transform.parent = gameObject.transform;
                g.GetComponent<MeshRenderer>().material.color = _colorList[_colorSelector%_colorList.Length];
                g.GetComponent<IsRendered>().SetCubePosition(newCube.X, newCube.Y, newCube.Z, newLod, pyriteQuery, this);

                g.transform.localScale = new Vector3(
                    pyriteLevel.WorldCubeScale.x,
                    pyriteLevel.WorldCubeScale.z,
                    pyriteLevel.WorldCubeScale.y);
                _colorSelector++;
                createdDetectors.Add(g);
            }
            registerCreatedDetectorCubes(createdDetectors);
            yield break;
        }

        public void EnqueueLoadCubeRequest(LoadCubeRequest loadRequest)
        {
            _loadCubeRequests.Enqueue(loadRequest);
        }

        private IEnumerator ProcessLoadCubeRequest(LoadCubeRequest loadRequest)
        {
            DebugLog("+LoadCube(L{3}:{0}_{1}_{2})", loadRequest.X, loadRequest.Y, loadRequest.Z, loadRequest.Lod);
            var modelPath = loadRequest.Query.GetModelPath(loadRequest.Lod, loadRequest.X, loadRequest.Y, loadRequest.Z);
            var pyriteLevel =
                loadRequest.Query.DetailLevels[loadRequest.Lod];

            var buffer = new GeometryBuffer();

            WWW loader;
            if (!UseEbo)
            {
                if (!_objCache.ContainsKey(modelPath))
                {
                    _objCache[modelPath] = null;
                    yield return StartCoroutine(StartRequest(modelPath));
                    if (!UseWww)
                    {
                        var client = new RestClient(modelPath);
                        var request = new RestRequest(Method.GET);
                        request.AddHeader("Accept-Encoding", "gzip, deflate");
                        client.ExecuteAsync(request, (r, h) =>
                        {
                            LogResponseError(r, modelPath);
                            if (r.RawBytes != null)
                            {
                                _objCache[modelPath] = r.Content;
                            }
                            else
                            {
                                Debug.LogError("Error getting model data");
                            }
                        });
                    }
                    else
                    {
                        loader = WwwExtensions.CreateWWW(modelPath + "?fmt=obj", true);
                        yield return loader;
                        LogWwwError(loader, modelPath);
                        _objCache[modelPath] = loader.GetDecompressedText();
                    }
                    while (_objCache[modelPath] == null)
                    {
                        yield return null;
                    }
                    EndRequest(modelPath);
                }
                while (_objCache[modelPath] == null)
                {
                    yield return null;
                }
                CubeBuilderHelpers.SetGeometryData(_objCache[modelPath], buffer);
            }
            else
            {
                if (!_eboCache.ContainsKey(modelPath))
                {
                    _eboCache[modelPath] = null;
                    yield return StartCoroutine(StartRequest(modelPath));
                    if (!UseWww)
                    {
                        var client = new RestClient(modelPath);
                        var request = new RestRequest(Method.GET);
                        client.ExecuteAsync(request, (r, h) =>
                        {
                            LogResponseError(r, modelPath);
                            if (r.RawBytes != null)
                            {
                                _eboCache[modelPath] = r.RawBytes;
                            }
                            else
                            {
                                Debug.LogError("Error getting model data");
                            }
                        });
                    }
                    else
                    {
                        loader = WwwExtensions.CreateWWW(modelPath);
                        yield return loader;
                        LogWwwError(loader, modelPath);
                        _eboCache[modelPath] = loader.GetDecompressedBytes();
                    }
                    while (_eboCache[modelPath] == null)
                    {
                        yield return null;
                    }
                    EndRequest(modelPath);
                }
                // Loop while other tasks finish getting ebo data
                while (_eboCache[modelPath] == null)
                {
                    // Another task is in the process of filling out this cache entry.
                    // Loop until it is set
                    yield return null;
                }
                buffer.EboBuffer = _eboCache[modelPath];
            }

            var textureCoordinates = pyriteLevel.TextureCoordinatesForCube(loadRequest.X, loadRequest.Y);
            var materialDataKey = string.Format("model.mtl_{0}_{1}_{2}", textureCoordinates.x, textureCoordinates.y,
                loadRequest.Lod);
            if (!_materialDataCache.ContainsKey(materialDataKey))
            {
                var materialData = new List<MaterialData>();
                _materialDataCache[materialDataKey] = null;
                CubeBuilderHelpers.SetDefaultMaterialData(materialData, (int) textureCoordinates.x,
                    (int) textureCoordinates.y, loadRequest.Lod);

                foreach (var m in materialData)
                {
                    var texturePath = loadRequest.Query.GetTexturePath(loadRequest.Query.GetLodKey(loadRequest.Lod),
                        (int) textureCoordinates.x,
                        (int) textureCoordinates.y);
                    if (!_textureCache.ContainsKey(texturePath))
                    {
                        // Set to null to signal to other tasks that the key is in the process
                        // of being filled
                        _textureCache[texturePath] = null;
                        yield return StartCoroutine(StartRequest(texturePath));
                        byte[] textureData = null;
                        if (!UseWww)
                        {
                            var client = new RestClient(texturePath);
                            var request = new RestRequest(Method.GET);
                            client.ExecuteAsync(request, (r, h) =>
                            {
                                LogResponseError(r, texturePath);
                                if (r.RawBytes != null)
                                {
                                    textureData = r.RawBytes;
                                }
                                else
                                {
                                    Debug.LogError("Error getting texture data");
                                }
                            });
                        }
                        else
                        {
                            // Do not request compression for textures
                            var texloader = WwwExtensions.CreateWWW(texturePath, false);
                            yield return texloader;
                            LogWwwError(texloader, texturePath);
                            textureData = texloader.bytes;
                        }

                        while (textureData == null)
                        {
                            yield return null;
                        }

                        var texture = new Texture2D(1, 1, TextureFormat.DXT1, false);
                        texture.LoadImage(textureData);
                        _textureCache[texturePath] = texture;
                        EndRequest(texturePath);
                    }

                    // Loop while other tasks finish creating texture
                    while (_textureCache[texturePath] == null)
                    {
                        // Another task is in the process of filling out this cache entry.
                        // Loop until it is set
                        yield return null;
                    }
                    m.DiffuseTex = _textureCache[texturePath];
                }
                _materialDataCache[materialDataKey] = materialData;
            }
            while (_materialDataCache[materialDataKey] == null)
            {
                // Another task is in the process of filling out this cache entry.
                // Loop until it is set
                yield return null;
            }
            Build(buffer, _materialDataCache[materialDataKey], loadRequest.X, loadRequest.Y, loadRequest.Z,
                loadRequest.Lod, loadRequest.RegisterCreatedObjects);
            DebugLog("-LoadCube(L{3}:{0}_{1}_{2})", loadRequest.X, loadRequest.Y, loadRequest.Z, loadRequest.Lod);
        }

        private void Build(GeometryBuffer buffer, List<MaterialData> materialData, int x, int y, int z, int lod,
            Action<GameObject[]> registerCreatedObjects)
        {
            var materials = new Dictionary<string, Material[]>();

            foreach (var md in materialData)
            {
                if (!_materialCache.ContainsKey(md.Name))
                {
                    _materialCache[md.Name] = CubeBuilderHelpers.GetMaterial(UseUnlitShader, md);
                }
                materials.Add(md.Name, _materialCache[md.Name]);
            }

            var ms = new GameObject[buffer.NumObjects];

            for (var i = 0; i < buffer.NumObjects; i++)
            {
                var go = new GameObject();
                go.name = String.Format("cube_L{4}:{0}_{1}_{2}.{3}", x, y, z, i, lod);
                go.transform.parent = gameObject.transform;
                go.AddComponent(typeof (MeshFilter));
                go.AddComponent(typeof (MeshRenderer));
                ms[i] = go;
            }

            if (registerCreatedObjects != null)
            {
                registerCreatedObjects(ms);
            }

            buffer.PopulateMeshes(ms, materials);
        }
    }
}