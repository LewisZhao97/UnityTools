using System.Collections.Generic;
using UnityEngine;

namespace Lewiszhao.Unitytools.Editor
{
    /// <summary>
    /// Serialized configuration asset for ramp map generation.
    /// <para>
    /// <see cref="RampMapData"/> is a <see cref="ScriptableObject"/> that stores
    /// all parameters required to generate a ramp texture using
    /// <c>RampMapCreator</c> editor tool.
    /// </para>
    /// <para>
    /// This asset acts as a reusable preset and allows users to:
    /// <list type="bullet">
    /// <item>Save multiple gradient rows</item>
    /// <item>Preserve ramp texture resolution settings</item>
    /// <item>Reload and iterate ramp designs efficiently</item>
    /// </list>
    /// </para>
    /// <para>
    /// Typically used in toon shading or stylized rendering workflows
    /// where ramp textures are frequently tweaked.
    /// </para>
    /// </summary>
    
    [CreateAssetMenu(
        fileName = "RampMapData",
        menuName = "Ramp Map Tools/Ramp Map Data",
        order = 120)]
    public class RampMapData : ScriptableObject
    {
        /// <summary>
        /// Output ramp texture name (without file extension).
        /// This name will be used when exporting the ramp texture.
        /// </summary>
        public string RampMapName = "NewRampMap";

        /// <summary>
        /// Width (in pixels) of each ramp row.
        /// </summary>
        public int RampMapWidth = 32;

        /// <summary>
        /// Height (in pixels) of each ramp row.
        /// </summary>
        public int RampMapHeight = 4;

        /// <summary>
        /// Gradient list used to generate ramp rows.
        /// <para>
        /// Each <see cref="Gradient"/> corresponds to one horizontal strip
        /// in the final ramp texture.
        /// </para>
        /// </summary>
        public List<Gradient> Gradients = new();
    }
}