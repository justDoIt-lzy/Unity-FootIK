using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestFootIK : MonoBehaviour
{
    public Animator playerAnimator;
    public HumanDescription humanDes;
    public bool pelvisAdjust;
    public float footHeightOffset = 1;
    [Range(0, 1)]
    public float leftIKPositionWeight = 1;
    [Range(0, 1)]
    public float rightIKPositionWeight = 1;
    public bool isIK = false;
    [Header("======================Gizmos setting===============================")]
    public float gizmosJointsRadius = 0.1f;

    private Vector3 _leftFootRayPos = Vector3.zero;
    private Vector3 _rightFootRayPos = Vector3.zero;

    private FootIKInfo _leftFootIKInfo;
    private FootIKInfo _rightFootIKInfo;

    private RaycastHit _raycastHitInfo;
    private float _rayMaxDistance = 1;

    private string _leftIKWeight = "LeftFootCurve";
    private string _rightIKWeight = "RightFootCurve";


    struct FootIKInfo
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    private void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("j"))
        {
            playerAnimator.SetBool("Walking", true);
        }

        if (Input.GetKeyDown("k"))
        {
            playerAnimator.SetBool("Walking", false);
        }
    }

    private void FixedUpdate()
    {
        getFootIkRayPos(HumanBodyBones.LeftFoot, ref _leftFootRayPos);
        getFootIkRayPos(HumanBodyBones.RightFoot, ref _rightFootRayPos);

        Vector3 leftFootForward;
        Vector3 rightFootForward;
        getFootForwardVector(HumanBodyBones.LeftFoot, out leftFootForward);
        getFootForwardVector(HumanBodyBones.RightFoot, out rightFootForward);

        //进行射线检测，计算footik pos
        raycastToGetFootIKInfo(_leftFootRayPos, leftFootForward, ref _leftFootIKInfo);
        raycastToGetFootIKInfo(_rightFootRayPos, rightFootForward, ref _rightFootIKInfo);
    }

    private void getFootIkRayPos(HumanBodyBones foot, ref Vector3 footPos)
    {
        footPos = playerAnimator.GetBoneTransform(foot).position;
        footPos.y += footHeightOffset;
    }

    private void getFootForwardVector(HumanBodyBones foot, out Vector3 forward)
    {
        forward = playerAnimator.GetBoneTransform(foot).forward;
    }

    private void raycastToGetFootIKInfo(Vector3 footRayPos, Vector3 forward, ref FootIKInfo IKInfo)
    {
        if(Physics.Raycast(footRayPos, Vector3.down, out _raycastHitInfo, footHeightOffset + _rayMaxDistance))
        {
            IKInfo.position = _raycastHitInfo.point;
            Vector3 xAxis = Vector3.Cross(_raycastHitInfo.normal, forward).normalized;
            Vector3 zAxis = Vector3.Cross(xAxis, _raycastHitInfo.normal).normalized;
            IKInfo.rotation = Quaternion.LookRotation(zAxis, _raycastHitInfo.normal);
        }
    }

    //调整pelvis，通过bodyposition
    private void movePelvis()
    {
        float lOffsetPosition = _leftFootIKInfo.position.y - transform.position.y;
        float rOffsetPosition = _rightFootIKInfo.position.y - transform.position.y;

        
        float totalOffset = (lOffsetPosition < rOffsetPosition) ? lOffsetPosition : rOffsetPosition;
        //因为bodyPosition每次获取都是动画片段计算后的，非之前调整过的。
        playerAnimator.bodyPosition += Vector3.up * totalOffset;
    }

    //根据ik点计算脚踝应该的高度，以及将动画本身的xz位移保留。
    private void moveFootToIKPoint(AvatarIKGoal foot, Vector3 positionIKHolder) 
    {
        //获取animator IK Goal 的 原本 pos
        //该原本的pos，可保留其x，z的位置信息，因为ik pos weight始终为1，需要位移信息，该原本的pos就提供了，是保证 ik pos weight始终为1，同时能走动的根本。
        Vector3 targetIKPosition = playerAnimator.GetIKPosition(foot);
        //1.第一种方法
        //calculateTargetIKpos1(ref targetIKPosition, ref positionIKHolder);

        //2.第二种方法
        calculateTargetIKpos2(ref targetIKPosition, positionIKHolder);
        playerAnimator.SetIKPosition(foot, targetIKPosition);
    }

    //-----------对脚步ik位置计算的算法---------
    //1.直接都转成transform下的坐标系
    private void calculateTargetIKpos1(ref Vector3 targetIKPosition, ref Vector3 positionIKHolder)
    {
        //1.坐标转换，转换成
        //把原本的IK Goal 的pos转到transform的坐标系（因为transform其实和root重合，所以可以认为是model space）
        targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
        //把期望的IK Goal 的pos转到transform的坐标系
        positionIKHolder = transform.InverseTransformPoint(positionIKHolder);

        //get ik goal 的位置信息是动画本身的，因此其本身y值本身就是脚踝对地的偏移，针对平面而言
        targetIKPosition.y += positionIKHolder.y;
        //把新的IK goal pos转到世界坐标系
        targetIKPosition = transform.TransformPoint(targetIKPosition);
    }

    //2.先将target转换为transform下，获得脚踝对平面的高度，然后直接将其设置到ik点，再加上脚踝对平面高度
    // 脚踝的ik点高度 = ik点高度+脚踝对平面的高度
    private void calculateTargetIKpos2(ref Vector3 targetIKPosition, in Vector3 positionIKHolder)
    {
        Vector3 localTargetIKPosition = transform.InverseTransformPoint(targetIKPosition);
        targetIKPosition.y = positionIKHolder.y;
        targetIKPosition.y += localTargetIKPosition.y;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!isIK)
        {
            return;
        }
        if (pelvisAdjust)
        {
            movePelvis();
        }

        //设置脚部ik目标点
        moveFootToIKPoint(AvatarIKGoal.LeftFoot, _leftFootIKInfo.position);
        moveFootToIKPoint(AvatarIKGoal.RightFoot, _rightFootIKInfo.position);
        //脚步为了不穿模，我采取了全程为1的做法。
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);

        Debug.Log($"左脚步rotation：{_leftFootIKInfo.rotation}，angle：{_leftFootIKInfo.rotation.eulerAngles}");
        Debug.Log($"右脚步rotation：{_rightFootIKInfo.rotation}，angle：{_rightFootIKInfo.rotation.eulerAngles}");
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftFoot, _leftFootIKInfo.rotation);
        playerAnimator.SetIKRotation(AvatarIKGoal.RightFoot, _rightFootIKInfo.rotation);
        //旋转通过曲线的变化控制weight，达到较好的效果
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, playerAnimator.GetFloat(_leftIKWeight));
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.RightFoot, playerAnimator.GetFloat(_rightIKWeight));
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(_leftFootRayPos, gizmosJointsRadius);
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(_rightFootRayPos, gizmosJointsRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(_leftFootRayPos, _leftFootRayPos + Vector3.down * (footHeightOffset + _rayMaxDistance));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(_rightFootRayPos, _rightFootRayPos + Vector3.down * (footHeightOffset + _rayMaxDistance));
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_leftFootIKInfo.position, gizmosJointsRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(_rightFootIKInfo.position, gizmosJointsRadius);
    }
}
