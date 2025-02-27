using Godot;

namespace WavefrontObjViewer.Scripts;

public partial class OrbitCamera3D : Camera3D
{
    // 目标
    [Export] private Node3D _orbitTarget;
    [Export(PropertyHint.Range, "0,10,1,or_greater")]
    private float Distance
    {
        get => Transform.Origin.Z;
        set => Transform = new Transform3D { Origin = new Vector3(0f, 0f, value), Basis = Transform.Basis };
    }
    // 鼠标灵敏度
    [ExportGroup("Sensitivity")]
    [Export] private float _mouseXSensitivity = 0.005f;
    [Export] private float _mouseYSensitivity = -0.005f;
    [Export(PropertyHint.Range, "-0.5,0.5,0.01")]
    private float _wheelSensitivity = 0.05f;

    private Node3D _pivotPoint;

    public override void _Ready()
    {
        _pivotPoint = GetParentNode3D();
        if (_orbitTarget != null)
        {
            _pivotPoint.Transform = _orbitTarget.Transform;
        }
        else
        {
            _orbitTarget = _pivotPoint;
        }
    }

    private bool _isLeftMouseButtonPressed;
    private bool _isMiddleMouseButtonPressed;
    private bool _isRightMouseButtonPressed;

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);

        // 处理鼠标按键事件
        if (@event is InputEventMouseButton mouseButtonEvent)
        {
            switch (mouseButtonEvent.ButtonIndex)
            {
                case MouseButton.Left:
                    _isLeftMouseButtonPressed = mouseButtonEvent.Pressed;
                    break;
                case MouseButton.Right:
                    _isRightMouseButtonPressed = mouseButtonEvent.Pressed;
                    break;
                case MouseButton.Middle:
                    _isMiddleMouseButtonPressed = mouseButtonEvent.Pressed;
                    break;
                case MouseButton.WheelDown:
                    if (Projection == ProjectionType.Orthogonal)
                    {
                        Size *= 1 + _wheelSensitivity;
                    }
                    else if (Projection == ProjectionType.Perspective)
                    {
                        Fov *= 1 + _wheelSensitivity;
                    }

                    break;
                case MouseButton.WheelUp:
                    if (Projection == ProjectionType.Orthogonal)
                    {
                        Size *= 1 - _wheelSensitivity;
                    }
                    else if (Projection == ProjectionType.Perspective)
                    {
                        Fov *= 1 - _wheelSensitivity;
                    }

                    break;
            }
        }

        // 处理鼠标移动事件
        if (_isLeftMouseButtonPressed && @event is InputEventMouseMotion mouseMotionEvent)
        {
            (float deltaX, float deltaY) = mouseMotionEvent.Relative;


            float xFactor = -1;
            if (GlobalTransform.Basis.Column2.Dot(GlobalPosition.DirectionTo(_pivotPoint.GlobalPosition)) > 0)
            {
                xFactor = -xFactor;
            }

            _pivotPoint.Transform =
                _pivotPoint.Transform.RotatedLocal(Vector3.Up, deltaX * _mouseXSensitivity * xFactor);
            _pivotPoint.Transform = _pivotPoint.Transform.RotatedLocal(Vector3.Right, deltaY * _mouseYSensitivity);
        }

        // 处理键盘事件
        if (@event is InputEventKey { Keycode: Key.Escape, Pressed: true })
        {
            GetTree().Quit();
        }
    }
}