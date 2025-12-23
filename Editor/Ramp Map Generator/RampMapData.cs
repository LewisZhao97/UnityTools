using System.Collections.Generic;
using UnityEngine;

namespace Lewiszhao.Unitytools.Editor
{
    [CreateAssetMenu(fileName = "RampMapData", menuName = "Ramp Map Tools/Ramp Map Data", order = 120)]
    public class RampMapData : ScriptableObject
    {
        public string RampMapName = "NewRampMap";
        public int RampMapWidth = 32;
        public int RampMapHeight = 4;
        public List<Gradient> Gradients = new();
    }
}