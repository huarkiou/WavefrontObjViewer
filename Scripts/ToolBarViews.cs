using Godot;

namespace WavefrontObjViewer.Scripts;

public partial class ToolBarViews : VBoxContainer
{
    [Export]
    private Button _buttonToggleView;
    [Export]
    private Button _buttonDefaultView;
    [Export]
    private Button _buttonFrontView;
    [Export]
    private Button _buttonLeftView;
    [Export]
    private Button _buttonTopView;
    [Export]
    private Button _buttonBackView;
    [Export]
    private Button _buttonRightView;
    [Export]
    private Button _buttonBottomView;
    [Export]
    private Node3D _cameraFocus;
    [Export]
    private OrbitCamera3D _orbitCamera3D;

    private void _ready()
    {
        if (_buttonToggleView != null)
        {
            _buttonToggleView.Pressed += ButtonToggleViewPressed;
        }

        if (_buttonDefaultView != null)
        {
            _buttonDefaultView.Pressed += ButtonDefaultViewPressed;
        }

        if (_buttonFrontView != null)
        {
            _buttonFrontView.Pressed += ButtonFrontViewPressed;
        }

        if (_buttonLeftView != null)
        {
            _buttonLeftView.Pressed += ButtonLeftViewPressed;
        }

        if (_buttonTopView != null)
        {
            _buttonTopView.Pressed += ButtonTopViewPressed;
        }

        if (_buttonBackView != null)
        {
            _buttonBackView.Pressed += ButtonBackViewPressed;
        }

        if (_buttonRightView != null)
        {
            _buttonRightView.Pressed += ButtonRightViewPressed;
        }

        if (_buttonBottomView != null)
        {
            _buttonBottomView.Pressed += ButtonBottomViewPressed;
        }
    }

    private void ButtonToggleViewPressed()
    {
        if (_orbitCamera3D != null)
        {
            if (_orbitCamera3D.Projection == Camera3D.ProjectionType.Orthogonal)
            {
                _orbitCamera3D.Projection = Camera3D.ProjectionType.Perspective;
                _buttonToggleView.Text = "透视视图";
            }
            else
            {
                _orbitCamera3D.Projection = Camera3D.ProjectionType.Orthogonal;
                _buttonToggleView.Text = "正交视图";
            }
        }
    }

    private void ButtonDefaultViewPressed()
    {
        if (_cameraFocus != null)
        {
            _cameraFocus.Rotation = new Vector3(0, 0, 0);
        }

        if (_orbitCamera3D != null)
        {
            if (_orbitCamera3D.Projection == Camera3D.ProjectionType.Perspective)
            {
                _orbitCamera3D.Fov = OrbitCamera3D.DefaultFov;
            }
            else
            {
                _orbitCamera3D.Size = OrbitCamera3D.DefaultSize;
            }
        }
    }

    private void ButtonFrontViewPressed()
    {
        if (_cameraFocus != null)
        {
            _cameraFocus.Rotation = new Vector3(0, 0, 0);
        }
    }

    private void ButtonLeftViewPressed()
    {
        if (_cameraFocus != null)
        {
            _cameraFocus.Rotation = new Vector3(0, float.DegreesToRadians(-90), 0);
        }
    }

    private void ButtonTopViewPressed()
    {
        if (_cameraFocus != null)
        {
            _cameraFocus.Rotation = new Vector3(float.DegreesToRadians(-90), 0, 0);
        }
    }

    private void ButtonBackViewPressed()
    {
        if (_cameraFocus != null)
        {
            _cameraFocus.Rotation = new Vector3(0, float.DegreesToRadians(180), 0);
        }
    }

    private void ButtonRightViewPressed()
    {
        if (_cameraFocus != null)
        {
            _cameraFocus.Rotation = new Vector3(0, float.DegreesToRadians(90), 0);
        }
    }

    private void ButtonBottomViewPressed()
    {
        if (_cameraFocus != null)
        {
            _cameraFocus.Rotation = new Vector3(float.DegreesToRadians(90), 0, 0);
        }
    }
}