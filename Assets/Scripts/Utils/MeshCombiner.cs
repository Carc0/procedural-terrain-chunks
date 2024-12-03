using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    // Combina las meshes. Creado de tal manera que se pudiera usar desde el Editor
    // En resumen primero crea SubMeshes por cada material que haya en los hijo del GameObject padre
    // Posteriormente junta las SubMeshes para generar la finalMesh
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MeshCombiner : MonoBehaviour
    {
        // Crea un mesh combinando los submeshes hijos de un GameObject
        public static void CombineMeshes(GameObject _gameObject)
        {
            Vector3 initLocalScale = _gameObject.transform.localScale;
            Quaternion initRotation = _gameObject.transform.rotation;
            Vector3 initPosition = _gameObject.transform.position;

            // Guarda el transform del GameObject y lo inicializa para que funcione correctamente el Mesh.CombineMeshes
            _gameObject.transform.localScale = Vector3.one;
            _gameObject.transform.rotation = Quaternion.identity;
            _gameObject.transform.position = Vector3.zero;

            MeshFilter[] meshFilters = _gameObject.GetComponentsInChildren<MeshFilter>(false);
            Material[] materials = GetChildrenMaterials(_gameObject);

            Mesh[] subMeshes = CreateSubMeshes(meshFilters, materials);

            CombineInstance[] combineInstances = CreateCombineInstances(subMeshes);
            Mesh finalMesh = CreateMesh(combineInstances, false, _gameObject.name);

            if (_gameObject.GetComponent<MeshFilter>() == null)
                _gameObject.AddComponent<MeshFilter>();
            _gameObject.GetComponent<MeshFilter>().sharedMesh = finalMesh;

            if (_gameObject.GetComponent<MeshRenderer>() == null)
                _gameObject.AddComponent<MeshRenderer>();
            _gameObject.GetComponent<MeshRenderer>().sharedMaterials = materials;

            _gameObject.SetActive(true);

            _gameObject.transform.localScale = initLocalScale;
            _gameObject.transform.rotation = initRotation;
            _gameObject.transform.position = initPosition;
        }

        // Recupera un array con todos los materiales de los hijos del GameObject
        private static Material[] GetChildrenMaterials(GameObject _gameObject)
        {
            MeshRenderer[] meshRenderers = _gameObject.GetComponentsInChildren<MeshRenderer>(false);
            List<Material> materials = new();

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (meshRenderer.transform == _gameObject.transform) continue;

                Material[] meshRendererMaterials = meshRenderer.sharedMaterials;

                foreach (Material meshRendererMaterial in meshRendererMaterials)
                {
                    if (materials.Contains(meshRendererMaterial)) continue;

                    materials.Add(meshRendererMaterial);
                }
            }

            return materials.ToArray();
        }

        // Crea SubMeshes juntando los Meshes por material de los hijos 
        private static Mesh[] CreateSubMeshes(MeshFilter[] _meshFilters, Material[] _materials)
        {
            List<Mesh> subMeshes = new();
            List<CombineInstance> combineInstances;

            foreach (Material material in _materials)
            {
                combineInstances = new();

                foreach (MeshFilter meshFilter in _meshFilters)
                {
                    if (!meshFilter.TryGetComponent<MeshRenderer>(out var meshRenderer)) continue;

                    Material[] meshFilterMaterial = meshRenderer.sharedMaterials;

                    for (int i = 0; i < meshFilterMaterial.Length; i++)
                    {
                        if (meshFilterMaterial[i] != material) continue;

                        CombineInstance combineInstance = new()
                        {
                            mesh = meshFilter.sharedMesh,
                            subMeshIndex = i,
                            transform = meshFilter.transform.localToWorldMatrix
                        };

                        combineInstances.Add(combineInstance);
                    }

                    meshFilter.gameObject.SetActive(false);
                }

                Mesh mesh = CreateMesh(combineInstances.ToArray(), true, material.name);
                subMeshes.Add(mesh);
            }

            return subMeshes.ToArray();
        }

        // Crea un array de CombineInstance de los SubMeshes
        private static CombineInstance[] CreateCombineInstances(Mesh[] _meshes)
        {
            List<CombineInstance> combineInstances = new();

            foreach (Mesh _mesh in _meshes)
            {
                CombineInstance combineInstance = new()
                {
                    mesh = _mesh,
                    transform = Matrix4x4.identity
                };

                combineInstances.Add(combineInstance);
            }

            return combineInstances.ToArray();
        }

        // Crea un Mesh a partir de Combine Instances. IndexFormat.UInt32 para poder crear grandes Meshes
        private static Mesh CreateMesh(CombineInstance[] _combineInstances, bool _mergeSubMeshes, string _name)
        {
            Mesh mesh = new()
            {
                name = $"{_name}_Mesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };

            mesh.CombineMeshes(_combineInstances, _mergeSubMeshes);

            return mesh;
        }
    }
}
