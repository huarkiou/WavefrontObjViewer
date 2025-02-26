using Godot;

namespace WavefrontObjViewer.Scripts;

public partial class UserCamera : Camera3D
{
    // 目标
    [Export] private Node3D _targetNode;
    [Export] private Node3D _yawNode;
    [Export] private Node3D _pitchNode;
    // 鼠标灵敏度
    [Export] private float _mouseXSensitivity = 0.005f;
    [Export] private float _mouseYSensitivity = 0.005f;
    [Export] private float _wheelSensitivity = 0.05f;
    // 鼠标反向
    [Export] private bool _flipXAxis = false;
    [Export] private bool _flipYAxis = false;
    [Export] private bool _flipWheel = false;


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
                    Size *= _flipWheel ? 1 - _wheelSensitivity : 1 + _wheelSensitivity;
                    break;
                case MouseButton.WheelUp:
                    Size *= _flipWheel ? 1 + _wheelSensitivity : 1 - _wheelSensitivity;
                    break;
            }
        }

        // 处理鼠标移动事件
        if (_isLeftMouseButtonPressed && @event is InputEventMouseMotion mouseMotionEvent)
        {
            (float deltaX, float deltaY) = mouseMotionEvent.Relative;
            _yawNode.Transform =
                _yawNode.Transform.RotatedLocal(Vector3.Up, deltaX * _mouseXSensitivity * (_flipXAxis ? 1 : -1));
            
            _pitchNode.Transform =
                _pitchNode.Transform.RotatedLocal(Vector3.Right, deltaY * _mouseYSensitivity * (_flipYAxis ? -1 : 1));
        }

        // 处理键盘事件
        if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.Escape && keyEvent.Pressed)
            {
                GetTree().Quit();
            }
        }
    }
}