using System;

using System.Timers;
using ThreeFingersDragOnWindows.src.utils;

namespace ThreeFingersDragOnWindows.src.touchpad;

public class ThreeFingersDrag
{
    private readonly Timer _dragEndTimer = new(50);
    private readonly Timer _oneFingerTimer = new(20);
    private Timer _nocontacttimer = new(10);
    private bool _isDragging;
    private ThreeFingersPoints _lastPoints = ThreeFingersPoints.Empty;
    private MousePoint _lastOneFingerPoint = MousePoint.Empty;
    private long _lastThreeFingersContact;
    private static long _timelastcontact;
    private bool _contacleng0left = true;
    private int _contacleng1 = 0;
    private int _contacleng2 = 0;
    public ThreeFingersDrag()
    {
        // Setup timer
        _dragEndTimer.Elapsed += (_, _) => CheckDragEnd();
        _oneFingerTimer.Elapsed += (_, _) =>
        {
            if (Ctms() - _lastThreeFingersContact > 250)
            {
                stopDrag();
            }
        };
        _nocontacttimer.Elapsed += (_, _) => stopDrag();
        _nocontacttimer.AutoReset = false;
        _dragEndTimer.AutoReset = false;
        _oneFingerTimer.AutoReset = false;
    }

    public void OnTouchpadContact(TouchpadContact[] contacts)
    {

        ThreeFingersPoints points = new(contacts);
        if (App.Prefs.twotoone)
        {
            _nocontacttimer.Stop();
            _nocontacttimer.Interval = 50;
            _nocontacttimer.Start();
            if (Ctms() - _timelastcontact < 30)
            {
                _contacleng0left = false;
            }
            else
            {
                _contacleng0left = true;
            }
            if (contacts.Length == 3)
            {
                _contacleng2++;     //indicates how many times(about 6-9ms a cycle) the contact has been reported
            }

            if (contacts.Length == 1 && _contacleng2 >= 4) //in case of misactivate two to one finger drag
            {
                _contacleng1++;
            }
            else
            {
                _contacleng1 = 0;
            }
            _timelastcontact = Ctms();
            if (_contacleng1 >= 2 && !_contacleng0left && _contacleng2 >= 4) // threefinger for 30ms , onefinger for 15ms
            {
                if (!_isDragging)
                {
                    _isDragging = true;
                    MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);
                    _lastThreeFingersContact = Ctms();
                    _lastPoints = points;
                }


            }





        }
        else
        {
            if (contacts.Length == 3)
            {

                if (!_isDragging)
                {
                    _isDragging = true;
                    MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);
                    _lastThreeFingersContact = Ctms();
                    _lastPoints = points;
                }
                else
                {
                    if (App.Prefs.ThreeFingersMove && _lastPoints != ThreeFingersPoints.Empty)
                    {

                        var dist2d = points.GetLongestDist2D(_lastPoints);
                        float elapsed = _lastThreeFingersContact == 0 ? 0 : Ctms() - _lastThreeFingersContact;
                        // Apply the Mouse Speed preference
                        dist2d.Multiply(App.Prefs.MouseSpeed / 60);

                        // Calculate the mouse velocity
                        var mouseVelocity = (float)Math.Max(0.2, Math.Min(dist2d.Length() / elapsed, 20));
                        if (float.IsNaN(mouseVelocity) || float.IsInfinity(mouseVelocity)) mouseVelocity = 1;

                        // Calculate the pointer velocity in function of the mouse velocity and the preference
                        var pointerVelocity = (float)(App.Prefs.MouseAcceleration / 10 * Math.Pow(mouseVelocity, 2) + 0.4 * mouseVelocity);
                        pointerVelocity = (float)Math.Max(0.4, Math.Min(pointerVelocity, 1.6)); // Clamp
                        if (App.Prefs.MouseAcceleration == 0) pointerVelocity = 1; // Disable acceleration
                                                                                   // Apply acceleration
                        dist2d.Multiply(pointerVelocity);

                        MouseOperations.ShiftCursorPosition(dist2d.x, dist2d.y);
                    }

                    _lastPoints = points;
                    _lastThreeFingersContact = Ctms();
                    _dragEndTimer.Stop();
                    _dragEndTimer.Interval = GetReleaseDelay();
                    _dragEndTimer.Start();


                }
                _lastOneFingerPoint = MousePoint.Empty;
            }
            else
            {
                if (!_isDragging) return;
                MousePoint point = new MousePoint(contacts[0].X, contacts[0].Y);

                if (!App.Prefs.AllowReleaseAndRestart) stopDrag();
                // When releasing the fingers, one finger or two can be detected during some milliseconds.
                else if (Ctms() - _lastThreeFingersContact > 50)
                {
                    // Using a timer in case only one finger is detected when re-grabing the element
                    if (_oneFingerTimer.Enabled && _lastOneFingerPoint != MousePoint.Empty
                                               && point.DistTo(_lastOneFingerPoint) / MouseSpeedFactor() is < 50 and > 5)
                    {
                        stopDrag();
                    }
                    else _oneFingerTimer.Enabled = true;
                }
                _lastPoints = ThreeFingersPoints.Empty;
                _lastOneFingerPoint = point;
            }
        }
    }

    private void CheckDragEnd()
    {
        // minus 15 to avoid bugs when the timer ends before the time elapsed
        if (_isDragging && Ctms() - _lastThreeFingersContact >= GetReleaseDelay() - 20)
        {

            stopDrag();
        }
    }

    private void stopDrag()
    {

        _contacleng2 = 0;
        _contacleng1 = 0;
        _contacleng0left = true;
        if (_isDragging)
        {
            _isDragging = false;
            MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
        }
    }

    private int GetReleaseDelay()
    {
        return App.Prefs.AllowReleaseAndRestart ? Math.Max(App.Prefs.ReleaseDelay, 50) : 50;
    }

    private long Ctms()
    {
        return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
    }

    private float MouseSpeedFactor()
    {
        return App.Prefs.MouseSpeed / 30;
    }
}