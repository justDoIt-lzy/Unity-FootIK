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

        //�������߼�⣬����footik pos
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

    //����pelvis��ͨ��bodyposition
    private void movePelvis()
    {
        float lOffsetPosition = _leftFootIKInfo.position.y - transform.position.y;
        float rOffsetPosition = _rightFootIKInfo.position.y - transform.position.y;

        
        float totalOffset = (lOffsetPosition < rOffsetPosition) ? lOffsetPosition : rOffsetPosition;
        //��ΪbodyPositionÿ�λ�ȡ���Ƕ���Ƭ�μ����ģ���֮ǰ�������ġ�
        playerAnimator.bodyPosition += Vector3.up * totalOffset;
    }

    //����ik��������Ӧ�õĸ߶ȣ��Լ������������xzλ�Ʊ�����
    private void moveFootToIKPoint(AvatarIKGoal foot, Vector3 positionIKHolder) 
    {
        //��ȡanimator IK Goal �� ԭ�� pos
        //��ԭ����pos���ɱ�����x��z��λ����Ϣ����Ϊik pos weightʼ��Ϊ1����Ҫλ����Ϣ����ԭ����pos���ṩ�ˣ��Ǳ�֤ ik pos weightʼ��Ϊ1��ͬʱ���߶��ĸ�����
        Vector3 targetIKPosition = playerAnimator.GetIKPosition(foot);
        //1.��һ�ַ���
        //calculateTargetIKpos1(ref targetIKPosition, ref positionIKHolder);

        //2.�ڶ��ַ���
        calculateTargetIKpos2(ref targetIKPosition, positionIKHolder);
        playerAnimator.SetIKPosition(foot, targetIKPosition);
    }

    //-----------�ԽŲ�ikλ�ü�����㷨---------
    //1.ֱ�Ӷ�ת��transform�µ�����ϵ
    private void calculateTargetIKpos1(ref Vector3 targetIKPosition, ref Vector3 positionIKHolder)
    {
        //1.����ת����ת����
        //��ԭ����IK Goal ��posת��transform������ϵ����Ϊtransform��ʵ��root�غϣ����Կ�����Ϊ��model space��
        targetIKPosition = transform.InverseTransformPoint(targetIKPosition);
        //��������IK Goal ��posת��transform������ϵ
        positionIKHolder = transform.InverseTransformPoint(positionIKHolder);

        //get ik goal ��λ����Ϣ�Ƕ�������ģ�����䱾��yֵ������ǽ��׶Եص�ƫ�ƣ����ƽ�����
        targetIKPosition.y += positionIKHolder.y;
        //���µ�IK goal posת����������ϵ
        targetIKPosition = transform.TransformPoint(targetIKPosition);
    }

    //2.�Ƚ�targetת��Ϊtransform�£���ý��׶�ƽ��ĸ߶ȣ�Ȼ��ֱ�ӽ������õ�ik�㣬�ټ��Ͻ��׶�ƽ��߶�
    // ���׵�ik��߶� = ik��߶�+���׶�ƽ��ĸ߶�
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

        //���ýŲ�ikĿ���
        moveFootToIKPoint(AvatarIKGoal.LeftFoot, _leftFootIKInfo.position);
        moveFootToIKPoint(AvatarIKGoal.RightFoot, _rightFootIKInfo.position);
        //�Ų�Ϊ�˲���ģ���Ҳ�ȡ��ȫ��Ϊ1��������
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);

        Debug.Log($"��Ų�rotation��{_leftFootIKInfo.rotation}��angle��{_leftFootIKInfo.rotation.eulerAngles}");
        Debug.Log($"�ҽŲ�rotation��{_rightFootIKInfo.rotation}��angle��{_rightFootIKInfo.rotation.eulerAngles}");
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftFoot, _leftFootIKInfo.rotation);
        playerAnimator.SetIKRotation(AvatarIKGoal.RightFoot, _rightFootIKInfo.rotation);
        //��תͨ�����ߵı仯����weight���ﵽ�Ϻõ�Ч��
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
