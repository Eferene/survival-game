using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class VolumeProfileController : MonoBehaviour
{
    [Header("Depth Parameters")]
    [SerializeField] private CinemachineCamera _camera;
    [SerializeField] private float _depth;
    [SerializeField] private float _fogDensity;

    [Header("Post Processing Volume")]
    [SerializeField] private Volume _postProcessingVolume;

    [Header("Post Processing Profiles")]
    [SerializeField] private VolumeProfile _surfaceVolumeProfile;
    [SerializeField] private VolumeProfile _underwaterVolumeProfile;


    private void Start()
    {
        RenderSettings.fogDensity = _fogDensity / 10;
        RenderSettings.fogColor = Color.cyan;
    }

    private void Update()
    {
        if (_camera.transform.position.y < _depth)
            EnableEffect(true);
        else
            EnableEffect(false);
    }

    private void EnableEffect(bool enable)
    {
        if (!enable)
        {
            RenderSettings.fog = false;
            _postProcessingVolume.profile = _surfaceVolumeProfile;
        }
        else
        {
            RenderSettings.fog = true;
            _postProcessingVolume.profile = _underwaterVolumeProfile;
        }
    }
}
