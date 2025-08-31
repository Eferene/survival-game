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

    private void Start()
    {
        TrueLoadingScreen();
    }

    public void CallBuildNavMesh()
    {
        StartCoroutine(BuildNavMesh());
        FalseLoadingScreen();
    }
    IEnumerator BuildNavMesh()
    {
        TrueLoadingScreen();
        yield return null;
        mainIsland.BuildNavMesh();
        yield return new WaitForSecondsRealtime(0.1f);
        Island1.BuildNavMesh();
        yield return new WaitForSecondsRealtime(0.1f);
        Island2.BuildNavMesh();
        yield return new WaitForSecondsRealtime(0.1f);
        Island3.BuildNavMesh();
        yield return new WaitForSecondsRealtime(0.1f);
        Island4.BuildNavMesh();
        yield return new WaitForSecondsRealtime(0.1f);
        FalseLoadingScreen();
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