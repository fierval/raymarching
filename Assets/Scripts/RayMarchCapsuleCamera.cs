using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera)), ExecuteAlways]
public class RayMarchCapsuleCamera : SceneViewFilter {
    [SerializeField]
    private Shader _shader;
    public float _maxDistance;
    public Color _mainColor;

    [Header("3D Objects")]
    public Vector4[] _capsule1 = new Vector4[2];

    [Header("Lighting")]
    public Transform _directionalLight;
    public Color _LightCol;
    public float _LightIntensity;

    [Header("Shading")]
    public float _ShadowIntensity;
    public float _ShadowPenumbra;
    public Vector2 _ShadowDistance;

    [Header("Raymarching")]
    [Range(0.1f, 0.0001f)]
    public float _Accuracy;
    [Range(64, 500)]
    public int _MaxIterations;


    public Material _raymarchMaterial {
        get {
            if (!_raymarchMat && _shader) {
                _raymarchMat = new Material(_shader);
                _raymarchMat.hideFlags = HideFlags.HideAndDontSave;
            }

            return _raymarchMat;
        }
    }
    Material _raymarchMat;

    public Camera _camera {
        get {
            if (!_cam) { _cam = GetComponent<Camera>(); }
            return _cam;
        }
    }

    Camera _cam;
    private void Awake() {
        if(!Application.isPlaying) { return; }

        _raymarchMat = new Material(_shader);
        _raymarchMat.hideFlags = HideFlags.HideAndDontSave;
        SetShaderProps();

    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (!_raymarchMaterial) {
            Graphics.Blit(source, destination);
            return;
        }

        if (!Application.isPlaying) {
            SetShaderProps();
        }

        _raymarchMaterial.SetTexture("_MainTex", source);

        RenderTexture.active = destination;
        GLRender();
    }

    private void GLRender() {
        GL.PushMatrix();
        GL.LoadOrtho();
        _raymarchMaterial.SetPass(0);
        GL.Begin(GL.QUADS);

        // BL
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f);
        // BR
        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f);
        // TR
        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f);
        // TL
        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f);

        GL.End();
        GL.PopMatrix();
    }

    private void SetShaderProps() {
        _raymarchMaterial.SetVector("_LightDir", _directionalLight ? _directionalLight.forward : Vector3.down);
        _raymarchMaterial.SetVectorArray("_CamFrustrum", CamFrustrum(_camera));
        _raymarchMaterial.SetMatrix("_CamToWorld", _camera.cameraToWorldMatrix);
        _raymarchMaterial.SetFloat("_maxDistance", _maxDistance);
        _raymarchMaterial.SetColor("_mainColor", _mainColor);
        _raymarchMaterial.SetColor("_LightCol", _LightCol);

        _raymarchMaterial.SetFloat("_LightIntensity", _LightIntensity);
        _raymarchMaterial.SetVector("_ShadowDistance", _ShadowDistance);
        _raymarchMaterial.SetFloat("_ShadowIntensity", _ShadowIntensity);
        _raymarchMaterial.SetFloat("_ShadowPenumbra", _ShadowPenumbra);

        _raymarchMaterial.SetFloat("_Accuracy", _Accuracy);
        _raymarchMaterial.SetInt("_MaxIterations", _MaxIterations);

        _raymarchMaterial.SetVectorArray("_capsule1", _capsule1);



    }

    protected Vector4[] CamFrustrum(Camera cam) {

        float fov = Mathf.Tan((cam.fieldOfView * 0.5f) * Mathf.Deg2Rad);
        float nearPlane = cam.nearClipPlane;

        Vector3 goUp = Vector3.up * fov;
        Vector3 goRight = Vector3.right * fov * cam.aspect;

        Vector3 TL = (-Vector3.forward * nearPlane - goRight + goUp);
        Vector3 TR = (-Vector3.forward * nearPlane + goRight + goUp);
        Vector3 BR = (-Vector3.forward * nearPlane + goRight - goUp);
        Vector3 BL = (-Vector3.forward * nearPlane - goRight - goUp);

        Vector4[] frustrum = new Vector4[4] { TL, TR, BR, BL };

        return frustrum;
    }
}
