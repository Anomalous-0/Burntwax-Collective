using System;
using UnityEngine;

namespace Burntwax
{

    [CreateAssetMenu(menuName = "SceneData", fileName = "New SceneData")]
    public class SceneDataSO : ScriptableObject
    {
        public string Name;
        public int index;
    }
}
