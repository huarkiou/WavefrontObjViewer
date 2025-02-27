using Godot;

namespace WavefrontObjViewer.Scripts;

public partial class OrbitCamera3D : Camera3D
{
    // 目标
    [ExportGroup("Orbit")]
    [Export] private Node3D _targetNode;
    [Export] private Node3D _yawNode;
    [Export] private Node3D _pitchNode;
    // 鼠标灵敏度
    [ExportGroup("Sensitivity")]
    [Export] private float _mouseXSensitivity = 0.005f;
    [Export] private float _mouseYSensitivity = -0.005f;
    [Export(PropertyHint.Range, "-0.5,0.5,0.01")]
    private float _wheelSensitivity = 0.05f;

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
                    Size *= 1 + _wheelSensitivity;
                    break;
                case MouseButton.WheelUp:
                    Size *= 1 - _wheelSensitivity;
                    break;
            }
        }

        // 处理鼠标移动事件
        if (_isLeftMouseButtonPressed && @event is InputEventMouseMotion mouseMotionEvent)
        {
            (float deltaX, float deltaY) = mouseMotionEvent.Relative;
            _pitchNode.Transform =
                _pitchNode.Transform.RotatedLocal(Vector3.Right, deltaY * _mouseYSensitivity);

            float xFactor = -1;
            if (_pitchNode.Transform.Basis.Column2.Dot(Vector3.Forward) > 0)
            {
                xFactor = -xFactor;
            }

            _yawNode.Transform =
                _yawNode.Transform.RotatedLocal(Vector3.Up, deltaX * _mouseXSensitivity * xFactor);
        }

        // 处理键盘事件
        if (@event is InputEventKey { Keycode: Key.Escape, Pressed: true })
        {
            GetTree().Quit();
        }
    }
}