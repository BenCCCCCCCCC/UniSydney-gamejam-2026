using UnityEngine;

namespace FairyTale.Core
{
    /// <summary>
    /// 场景2 的入场控制。场景一加载完成（此刻还在黑幕下）时，
    /// 把猎人瞬移到画面左侧外的起点，再让他往右走入，
    /// 制造"从上一幕连续走过来"的错觉。挂在场景2 里一个空物体上。
    /// </summary>
    public class SceneIntroController : MonoBehaviour
    {
        [Tooltip("要入场的角色（猎人）")]
        public Transform actor;

        [Tooltip("入场起点：放在画面左边缘再往左一点的位置")]
        public Transform entryPoint;

        private void Start()
        {
            // 1. 先把猎人瞬移到左侧起点。此刻屏幕还是黑的，玩家看不到这次瞬移。
            if (actor != null && entryPoint != null)
                actor.position = entryPoint.position;

            // 2. 触发往右走入。
            // ⚠️ 这里要调用组员B 已有的行走逻辑，别自己重写一套走路。
            //    按 B 的真实接口替换下面这行，例如：
            //    actor.GetComponent<ActorWalker>().WalkToEnd();
            //    或者：actor.GetComponent<HunterMover>().StartWalk();
            // TODO: 接入 B 的行走方法
        }
    }
}
