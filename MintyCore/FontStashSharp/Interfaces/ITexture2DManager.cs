using System.Drawing;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.UI;

namespace MintyCore.FontStashSharp.Interfaces
{
    /// <summary>
    /// Texture Creation Service
    /// </summary>
    public interface ITexture2DManager
    {
        /// <summary>
        /// Creates a texture of the specified size
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        FontTextureWrapper CreateTexture(int width, int height);

        /// <summary>
        /// Returns size of the specified texture
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        Point GetTextureSize(object texture);

        /// <summary>
        /// Sets RGBA data at the specified bounds
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="data"></param>
        void SetTextureData(object texture, Rectangle bounds, byte[] data);
    }
}