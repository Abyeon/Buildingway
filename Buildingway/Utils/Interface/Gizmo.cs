using System.Numerics;
using Anyder.Objects;
using Dalamud.Bindings.ImGui;
using Dalamud.Bindings.ImGuizmo;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using CameraManager = FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager;

namespace Buildingway.Utils.Interface;

public static unsafe class Gizmo
{
    private static Camera* Camera => &CameraManager.Instance()->GetActiveCamera()->CameraBase.SceneCamera;

    public static ImGuizmoOperation Operation = ImGuizmoOperation.Translate;

    public static bool Manipulate(ref Transform transform, float snapDistance, string id)
    {
        ImGuizmo.BeginFrame();
        
        var cam = Camera->RenderCamera;
        var view = Camera->ViewMatrix;
        var proj = cam->ProjectionMatrix;

        var far = cam->FarPlane;
        var near = cam->NearPlane;
        var clip = far / (far - near);
        
        proj.M43 = -(clip * near);
        proj.M33 = -((far + near) / (far - near));
        view.M44 = 1.0f;

        ImGuizmo.SetDrawlist(ImGui.GetWindowDrawList());
        ImGuizmo.Enable(true);
        ImGuizmo.SetID((int)ImGui.GetID(id));
        ImGuizmo.SetOrthographic(false);

        Vector2 windowPos = ImGui.GetWindowPos();
        ImGuiIOPtr io = ImGui.GetIO();
        
        ImGuizmo.SetRect(windowPos.X, windowPos.Y, io.DisplaySize.X, io.DisplaySize.Y);

        Matrix4x4 matrix = transform.GetTransformation();
        Vector3 snap = Vector3.One * snapDistance;
        
        FixedManipulate(ref view.M11, ref proj.M11, Operation, ImGuizmoMode.Local, ref matrix.M11, ref snap.X);
        
        if (ImGuizmo.IsUsing())
        {
            Matrix4x4.Decompose(matrix, out var scale, out var rotation, out var translation);

            switch (Operation)
            {
                case ImGuizmoOperation.Translate:
                    transform.Position = translation;
                    break;
                case ImGuizmoOperation.Rotate:
                    transform.Rotation = rotation;
                    break;
                case ImGuizmoOperation.Scale:
                    transform.Scale = scale;
                    break;
            }

            return true;
        }

        return false;
    }

    private static void FixedGrid(ref float view, ref float proj, ref float matrix, float size)
    {
        fixed (float* nativeView = &view)
        {
            fixed (float* nativeProj = &proj)
            {
                fixed (float* nativeMatrix = &matrix)
                {
                    ImGuizmo.DrawGrid(nativeView, nativeProj, nativeMatrix, size);
                }
            }
        }
    }
    
    private static bool FixedManipulate(ref float view, ref float proj, ImGuizmoOperation op, ImGuizmoMode mode, ref float matrix, ref float snap)
    {
        fixed (float* nativeView = &view)
        {
            fixed (float* nativeProj = &proj)
            {
                fixed (float* nativeMatrix = &matrix)
                {
                    fixed (float* nativeSnap = &snap)
                    {
                        // Use the ImGuizmo.Manipulate method with proper parameters
                        return ImGuizmo.Manipulate(
                            nativeView,
                            nativeProj,
                            op,
                            mode,
                            nativeMatrix,
                            null,
                            nativeSnap
                        );
                    }
                }
            }
        }
    }
}
