using UnityEngine;

namespace XiaoZhi.Unity
{
    public class TransformFollower : MonoBehaviour
    {
        [SerializeField] private Vector3 _position;
        [SerializeField] private Quaternion _rotation;
        
        private Camera _camera;

        public void SetFollower(Camera cam)
        {
            _camera = cam;
        }

        public void LateUpdate()
        {
            if (!_camera) return;
            if (_camera.orthographic)
            {
                var finalMatrix = transform.localToWorldMatrix * Matrix4x4.TRS(_position, _rotation, Vector3.one);
                var finalPos = finalMatrix.GetPosition();
                _camera.orthographicSize = Mathf.Max(Mathf.Abs(finalPos.z), 0.1f);
                _camera.transform.position = new Vector3(finalPos.x, finalPos.y, 0);
                _camera.transform.rotation = finalMatrix.rotation;
            }
            else
            {
                var finalMatrix = transform.localToWorldMatrix * Matrix4x4.TRS(_position, _rotation, Vector3.one);
                _camera.transform.position = finalMatrix.GetPosition();
                _camera.transform.rotation = finalMatrix.rotation;   
            }
        }
    }
}