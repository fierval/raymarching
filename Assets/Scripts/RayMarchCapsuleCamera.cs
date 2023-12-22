using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera)), ExecuteAlways]
public class RayMarchCamera : SceneViewFilter {
    [SerializeField]
    private Shader _shader;
    public float _maxDistance;
    public Color _mainColor;

    [Header("3D Objects")]
    public Vector4 _sphere1 = new Vector4(0, 0, 0, 2);
    public Vector4 _box1 = new Vector4(0, 0, 0, 2);
    public Vector4 _sphere2 = new Vector4(0, 0, 0, 2);

    [Header("Smoothing")]
    public float _boxSphereSmooth;
    public float _sphereInteresectSmooth;
    public float _box1Round;

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

    [Header("Ambient Occlusion")]
    [Range(1, 5)]
    public int _AmbientOcclusionIterations;
    [Range(0.01f, 10f)]
    public float _AmbientOcclusionStepSize;
    [Range(0,1)]
    public float _AmbientOcclusionIntensity;

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
        _raymarchMaterial.SetMatrix("_CamFrustrum", CamFrustrum(_camera));
        _raymarchMaterial.SetMatrix("_CamToWorld", _camera.cameraToWorldMatrix);
        _raymarchMaterial.SetFloat("_maxDistance", _maxDistance);
        _raymarchMaterial.SetVector("_sphere1", _sphere1);
        _raymarchMaterial.SetVector("_sphere2", _sphere2);
        _raymarchMaterial.SetVector("_box1", _box1);
        _raymarchMaterial.SetColor("_mainColor", _mainColor);
        _raymarchMaterial.SetColor("_LightCol", _LightCol);

        _raymarchMaterial.SetFloat("_box1Round", _box1Round);
        _raymarchMaterial.SetFloat("_boxSphereSmooth", _boxSphereSmooth);
        _raymarchMaterial.SetFloat("_sphereInteresectSmooth", _sphereInteresectSmooth);

        _raymarchMaterial.SetFloat("_LightIntensity", _LightIntensity);
        _raymarchMaterial.SetVector("_ShadowDistance", _ShadowDistance);
        _raymarchMaterial.SetFloat("_ShadowIntensity", _ShadowIntensity);
        _raymarchMaterial.SetFloat("_ShadowPenumbra", _ShadowPenumbra);

        _raymarchMaterial.SetFloat("_Accuracy", _Accuracy);
        _raymarchMaterial.SetInt("_MaxIterations", _MaxIterations);

        _raymarchMaterial.SetFloat("_AmbientOcclusionStepSize", _AmbientOcclusionStepSize);
        _raymarchMaterial.SetFloat("_AmbientOcclusionIntensity", _AmbientOcclusionIntensity);
        _raymarchMaterial.SetInt("_AmbientOcclusionIterations", _AmbientOcclusionIterations);

    }

    private Matrix4x4 CamFrustrum(Camera cam) {
        Matrix4x4 frustrum = Matrix4x4.identity;
        float fov = Mathf.Tan((cam.fieldOfView * 0.5f) * Mathf.Deg2Rad);
        float nearPlane = cam.nearClipPlane;

        Vector3 goUp = Vector3.up * fov;
        Vector3 goRight = Vector3.right * fov * cam.aspect;

        Vector3 TL = (-Vector3.forward * nearPlane - goRight + goUp);
        Vector3 TR = (-Vector3.forward * nearPlane + goRight + goUp);
        Vector3 BR = (-Vector3.forward * nearPlane + goRight - goUp);
        Vector3 BL = (-Vector3.forward * nearPlane - goRight - goUp);

        frustrum.SetRow(0, TL);
        frustrum.SetRow(1, TR);
        frustrum.SetRow(2, BR);
        frustrum.SetRow(3, BL);

        return frustrum;
    }
}
