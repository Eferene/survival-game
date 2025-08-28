using System.Collections;
using Unity.AI.Navigation; // Bu using önemli
using UnityEngine;

public class NavMeshGenerator : MonoBehaviour
{
    [SerializeField] private NavMeshSurface mainIsland;

    // Bu metodu event'ten çağıracağız.
    public void GenerateNavMeshAsync()
    {
        StartCoroutine(UpdateNavMeshCoroutine());
    }

    private IEnumerator UpdateNavMeshCoroutine()
    {
        Debug.Log("Asenkron NavMesh bake işlemi başlıyor, chill bro...");

        // İşte sihirli kısım bu. UpdateNavMesh asenkron çalışır.
        AsyncOperation operation = mainIsland.UpdateNavMesh(mainIsland.navMeshData);

        // Bake işlemi bitene kadar bekle.
        while (!operation.isDone)
        {
            // İstersen burada operation.progress ile bir loading bar bile doldurabilirsin.
            yield return null;
        }
        Debug.Log("NavMesh Bake tamamdır! Agents are good to go.");
    }
}