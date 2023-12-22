using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[RequireComponent(typeof(Camera)), ExecuteAlways]
public class RayTraceCamera : SceneViewFilter {

    public Shader _shader;

    public float _maxDistance;
    public Color _mainColor;
    
    [Header("3D Objects")]
    public Vector4 _sphere1 = new Vector4(0, 0, 0, 2);

    [Header("Lighting")]
    public Transform _directionalLight;
    public Color _LightCol;
    public float _LightIntensity;


    public Material _rayTraceMaterial {
        get {
            if (!_raytraceMat && _shader) {
                _raytraceMat = new Material(_shader);
                _raytraceMat.hideFlags = HideFlags.HideAndDontSave;
            }

            return _raytraceMat;
        }
    }
    Material _raytraceMat;

    public Camera _camera {
        get {
            if (!_cam) { _cam = GetComponent<Camera>(); }
            return _cam;
        }
    }

    Camera _cam;
    private void Awake() {
        if(!Application.isPlaying) { return; }

        _raytraceMat = new Material(_shader);
        _raytraceMat.hideFlags = HideFlags.HideAndDontSave;
        SetShaderProps();

    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination) {
        if (!_rayTraceMaterial) {
            Graphics.Blit(source, destination);
            return;
        }

        if (!Application.isPlaying) {
            SetShaderProps();
        }

        _rayTraceMaterial.SetTexture("_MainTex", source);

        RenderTexture.active = destination;
        GLRender();
    }

    private void GLRender() {
        GL.PushMatrix();
        GL.LoadOrtho();
        _rayTraceMaterial.SetPass(0);
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

    protected void SetShaderProps() {
        _rayTraceMaterial.SetVectorArray("_CamFrustrum", CamFrustrum(_camera));
        _rayTraceMaterial.SetMatrix("_CamToWorld", _camera.cameraToWorldMatrix);
        _rayTraceMaterial.SetFloat("_maxDistance", _maxDistance);
        _rayTraceMaterial.SetColor("_mainColor", _mainColor);
        _rayTraceMaterial.SetVector("_sphere1", _sphere1);

        // Lighting
        _rayTraceMaterial.SetFloat("_LightIntensity", _LightIntensity);
        _rayTraceMaterial.SetVector("_LightDir", _directionalLight? _directionalLight.forward : Vector3.down);
        _rayTraceMaterial.SetColor("_LightCol", _LightCol);
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

        Vector4[] frustrum = new Vector4[4] {TL, TR, BR, BL};

        return frustrum;
    }
}
