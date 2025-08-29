using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshGenerator : MonoBehaviour
{
    [SerializeField] private NavMeshSurface mainIsland;
    [SerializeField] private NavMeshSurface Island1;
    [SerializeField] private NavMeshSurface Island2;
    [SerializeField] private NavMeshSurface Island3;
    [SerializeField] private NavMeshSurface Island4;

    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject date;

    public void CallBuildNavMesh()
    {
        //StartCoroutine(BuildNavMesh());
        mainIsland.BuildNavMesh();
    }
    IEnumerator BuildNavMesh()
    {
        //TrueLoadingScreen();
        mainIsland.BuildNavMesh();
        yield return new WaitForSecondsRealtime(0.2f);
        Island1.BuildNavMesh();
        yield return new WaitForSecondsRealtime(0.1f);
        Island2.BuildNavMesh();
        yield return new WaitForSecondsRealtime(0.1f);
        Island3.BuildNavMesh();
        yield return new WaitForSecondsRealtime(0.1f);
        Island4.BuildNavMesh();
        //yield return new WaitForSecondsRealtime(0.1f);
        //FalseLoadingScreen();
    }

    void TrueLoadingScreen()
    {
        panel.SetActive(true);
        date.SetActive(false);
    }

    void FalseLoadingScreen()
    {
        panel.SetActive(false);
        date.SetActive(true);
    }
}