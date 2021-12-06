using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;

public class CarAgent : Agent
{
    public new Transform transform;
    public ArcadeCar car_movement;
    [Space]
    public ParkingLot lot;
    public int observe_rays;
    public LayerMask car_ignore;
    [Space]
    public Vector3 start_position;
    public Vector3 start_rotation;

    #region Mono
    private void Start()
    {
        observe_angle_step = 360 / observe_rays;

        ReceiveParkFeedback = (float angle) =>
        {
            park_angle = angle;
        };
    }

    public const float STAY_TIME = 1f;

    Action<float> ReceiveParkFeedback;
    float lot_dst = 0, park_angle = 0, stay_timer;
    bool last_park_status = false;

    private void Update()
    {
        lot_dst = (lot.transform.position - transform.position).magnitude;
        bool park_status = lot.CheckParkStatus(this, lot_dst, ReceiveParkFeedback);

        if (park_status)
        {
            if (park_status != last_park_status) AddReward(0.5f);

            stay_timer -= Time.deltaTime;

            if (stay_timer <= 0)
            {
                float dst_bonus = (ParkingLot.MAX_PARK_DISTANCE - lot_dst) / (ParkingLot.MAX_PARK_DISTANCE * 2),
                angle_bonus = (ParkingLot.MAX_PARK_ANGLE - park_angle) / (ParkingLot.MAX_PARK_ANGLE * 2);

                AddReward(dst_bonus + angle_bonus);
                EndEpisode();
            }
        }
        else stay_timer = STAY_TIME;

        last_park_status = park_status;
    }

    public const string ENV_TAG = "Environment";
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag == ENV_TAG)
            AddReward(-1);
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.transform.tag == ENV_TAG)
            AddReward(-0.025f);
    }
    #endregion

    #region MLAgents
    public override void OnEpisodeBegin()
    {
        Vector3 pos = start_position + (Vector3.right * (UnityEngine.Random.value * 2 + 1) * 1.5f);
        car_movement.Reset(pos, -90);

        //Debug.Log(CompletedEpisodes.ToString());
    }

    private float previous_lot_dst;
    public override void OnActionReceived(ActionBuffers actions)
    {
        car_movement.direction.x = actions.DiscreteActions[0] - 1;
        car_movement.direction.y = actions.DiscreteActions[1] - 1;

        if (StepCount % 10 == 0)
            AddReward(lot_dst <= previous_lot_dst ? 0.01f : -lot_dst / 100.0f);
    }

    const int OBSERVER_DST = 20;

    int observe_angle_step = 0;
    List<float> observe_rays_dists = new List<float>();
    List<Vector3> observe_rays_dirs = new List<Vector3>();

    public override void CollectObservations(VectorSensor sensor)
    {
        float car_angle = transform.rotation.eulerAngles.y;

        observe_rays_dists = new List<float>();
        observe_rays_dirs = new List<Vector3>();

        Vector3 origin = transform.position;

        sensor.AddObservation(RemoveYAxes(lot.transform.position - origin));
        sensor.AddObservation(transform.rotation.eulerAngles.y);
        sensor.AddObservation(last_park_status ? 0 : 1);

        for (int i = 0; i < observe_rays; i++)
        {
            float angle = i * observe_angle_step - car_angle;
            float sin = Mathf.Sin(angle * Mathf.Deg2Rad),
                cos = Mathf.Cos(angle * Mathf.Deg2Rad);
            Vector3 global_direction = new Vector3(cos, 0, sin);

            Physics.Raycast(origin, global_direction, out RaycastHit hit, OBSERVER_DST, car_ignore);

            if (hit.transform)
            {
                float dst = hit.distance;
                sensor.AddObservation(dst);
                observe_rays_dists.Add(dst);
            }
            else
            {
                sensor.AddObservation(OBSERVER_DST);
                observe_rays_dists.Add(OBSERVER_DST);
            }

            observe_rays_dirs.Add(global_direction);
        }

        Vector2 RemoveYAxes(Vector3 a) => new Vector2(a.x, a.z);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> input = actionsOut.DiscreteActions;
        input[0] = (int)Input.GetAxisRaw("Horizontal") + 1;
        input[1] = (int)Input.GetAxisRaw("Vertical") + 1;
    }
    #endregion

    #region Other
    [ContextMenu("Set new start transform")]
    public void SetNewStartTransform()
    {
        start_position = transform.position;
        start_rotation = transform.rotation.eulerAngles;
    }
    #endregion

    #region Debug
    private void OnDrawGizmos()
    {
        for (int i = 0; i < Mathf.Min(observe_rays_dirs.Count, observe_rays_dists.Count); i++)
        {
            float dst = observe_rays_dists[i];
            Vector3 dir = observe_rays_dirs[i];

            Gizmos.color = Color.Lerp(Color.red, Color.green, dst / OBSERVER_DST);
            Gizmos.DrawRay(transform.position, dir);
        }
    }
    #endregion
}