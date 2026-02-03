using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Script.Utils;
using BetterGenshinImpact.GameTask.Common;
using OpenCvSharp;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Script.Dependence;

public class OcrBridge(string workDir)
{
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private static int _counter = -1;
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

    /// <summary>
    /// 截取屏幕并生成 OCR 训练数据
    /// </summary>
    /// <param name="relativePath">存储相对路径</param>
    public void GenerateOcrTrainingData(string relativePath)
    {
        // 1. 立即截图，确保时机准确
        var imageRegion = TaskControl.CaptureToRectArea();
        if (imageRegion == null) return;
        var matClone = imageRegion.SrcMat.Clone();
        imageRegion.Dispose();

        // 2. 异步队列处理
        _ = Task.Run(async () =>
        {
            await _lock.WaitAsync();
            try
            {
                string fullPath = ScriptUtils.NormalizePath(workDir, relativePath);
                if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
                if (_counter == -1) _counter = GetNextIndex(fullPath);

                // 执行 OCR 识别
                var result = OcrFactory.Paddle.OcrResult(matClone);
                string txtPath = Path.Combine(fullPath, "train.txt");

                foreach (var region in result.Regions)
                {
                    // 过滤置信度过低 (60%) 或无效文本
                    if (string.IsNullOrWhiteSpace(region.Text) || region.Score < 0.7f) continue;

                    // 3. 计算紧凑的切图区域
                    var rr = region.Rect;
                    // 使用 1.4 的反向缩减（比之前的 2.0 更保守，防止切过头）
                    float reverseUnclip = 1.35f; 
                    float minEdge = Math.Min(rr.Size.Width, rr.Size.Height);
                    // 根据项目中 Det.cs 的公式反推一部分偏移量
                    float offset = (minEdge / 3.0f) * reverseUnclip; 

                    var tightSize = new Size2f(
                        Math.Max(1, rr.Size.Width - offset), 
                        Math.Max(1, rr.Size.Height - offset)
                    );
                    
                    // 获取正交外接矩形（不旋转，直接切）
                    var rect = new RotatedRect(rr.Center, tightSize, rr.Angle).BoundingRect();
                    
                    // 边界保护
                    rect.X = Math.Max(0, rect.X);
                    rect.Y = Math.Max(0, rect.Y);
                    rect.Width = Math.Min(matClone.Cols - rect.X, rect.Width);
                    rect.Height = Math.Min(matClone.Rows - rect.Y, rect.Height);

                    if (rect.Width <= 0 || rect.Height <= 0) continue;

                    // 4. 保存图片和标签
                    string fileName = $"train_{_counter:D6}.png";
                    using var cropped = new Mat(matClone, rect);
                    Cv2.ImWrite(Path.Combine(fullPath, fileName), cropped);

                    await File.AppendAllTextAsync(txtPath, $"{fileName}\t{region.Text}\n", Utf8WithoutBom);
                    _counter++;
                }
                
                TaskControl.Logger.LogInformation("GenerateOcrTrainingData: 已识别并保存 {Count} 个区块到 {Path}", result.Regions.Length, relativePath);
            }
            catch (Exception ex)
            {
                TaskControl.Logger.LogError(ex, "GenerateOcrTrainingData 处理异常");
            }
            finally
            {
                _lock.Release();
                matClone.Dispose();
            }
        });
    }

    private int GetNextIndex(string directory)
    {
        try
        {
            var files = Directory.GetFiles(directory, "train_*.png");
            if (files.Length == 0) return 0;

            var maxIndex = files
                .Select(Path.GetFileNameWithoutExtension)
                .Select(n => n?.Length >= 6 ? n.Substring(6) : "0")
                .Select(s => int.TryParse(s, out var i) ? i : -1)
                .Max();
            return maxIndex + 1;
        }
        catch
        {
            return 0;
        }
    }
}
