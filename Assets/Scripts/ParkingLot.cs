using System;
using UnityEngine;

public class ParkingLot : MonoBehaviour
{
    public Transform car;
    [Space]
    public new SpriteRenderer renderer;
    public Color success_color, fail_color;

    public const float MAX_PARK_DISTANCE = 2.25f;
    public const float MAX_PARK_ANGLE = 25f;

    public bool CheckParkStatus(CarAgent car, float precalculated_dst, Action<float> park_feedback)
    {
        Transform car_transform = car.transform;
        float distance = precalculated_dst,
            angle1 = Vector3.Angle(car_transform.forward, -transform.up),
            angle2 = Vector3.Angle(car_transform.forward, transform.up),
            angle = Mathf.Min(angle1, angle2);

        if (distance <= MAX_PARK_DISTANCE && angle <= MAX_PARK_ANGLE)
        {
            OnParkSuccess();
            park_feedback?.Invoke(angle);

            return true;
        }
        else
        {
            OnParkFail();
            renderer.color = fail_color;

            return false;
        }
    }

    public void OnParkSuccess()
    {
        renderer.color = success_color;
    }

    public void OnParkFail()
    {
        renderer.color = fail_color;
    }
}