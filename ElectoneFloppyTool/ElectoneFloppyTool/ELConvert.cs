using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ElectoneFloppyTool
{
    public static class ELConvert
    {
        /// <summary>
        /// 將HD-COPY製作的*.IMG檔案轉換為標準虛擬軟碟格式(*.vfd)檔案
        /// </summary>
        /// <param name="imgFile">HD-COPY映像檔案(IMG)的位元組陣列</param>
        /// <returns>標準虛擬軟碟(VFD)的位元組陣列。</returns>
        public static async Task<byte[]> ConvertImgToVfdAsync(byte[] imgFile)
        {
            try
            {
                return await Task.Run(() =>
                {
                    byte[] tmpFmtInfo = new byte[2];
                    byte[] tmpImgContent;

                    List<byte> tmpResult = new List<byte>();

                    Array.Copy(imgFile, 0, tmpFmtInfo, 0, 2);

                    // 分辨HDCopy 1.7前和2.0後的格式，取得主要的內容
                    if (BitConverter.ToString(tmpFmtInfo) == BitConverter.ToString(new byte[2] { 0xFF, 0x18 }))
                    {
                        tmpImgContent = new byte[imgFile.Length - 0x0E];
                        Array.Copy(imgFile, 0x0E, tmpImgContent, 0x0, tmpImgContent.Length);
                    }
                    else
                    {
                        tmpImgContent = imgFile;
                    }

                    // 開始解碼
                    int offset = 0x0;
                    int maxTrackLen = 0; // 最大Track數，從0開始
                    int secPerTrack = 18; // 每Track的Sector數，2HD為18
                    bool[] notEmptyTrack = new bool[168]; // 每個Head是否有壓縮資料，每Track兩個Head

                    // 0x00 取得最大Track數
                    maxTrackLen = tmpImgContent[offset++];

                    // 0x01 取得每Track的Sector數
                    secPerTrack = tmpImgContent[offset++];

                    // 0x02~0xA9 取得每個Head是否有壓縮資料
                    for (int h = 0; h < 168; h++)
                    {
                        notEmptyTrack[h] = Convert.ToBoolean(tmpImgContent[offset + h]);
                    }
                    offset += 168;

                    // 0xAA~ 解碼RLE壓縮的內容
                    for (int t = 0; t <= maxTrackLen; t++) // 跑每個Track
                    {
                        // 跑每個Head，一個Track有兩個Head
                        for (int h = 0; h < 2; h++)
                        {
                            // 如果這個Head沒有資料，則跳過
                            if (!notEmptyTrack[(t * 2) + h])
                            {
                                tmpResult.AddRange(new byte[0x200 * secPerTrack]);
                                continue;
                            }

                            // 取得這個Head壓縮後的資料長度
                            int tmpDataLen = 0;
                            tmpDataLen = BitConverter.ToInt16(tmpImgContent, offset);
                            offset += 2;

                            // 將壓縮資料解碼
                            byte escByte = 0x0; // RLE壓縮的Escape Byte
                            for (int l = 0; l < tmpDataLen; l++)
                            {
                                if (l == 0)
                                    escByte = tmpImgContent[offset]; // 每個資料塊的第一個位元組紀錄Escape Byte
                                else
                                {
                                    // 遇到Escape Byte時
                                    if (tmpImgContent[offset + l] == escByte)
                                    {
                                        l++;
                                        byte repeatByte = tmpImgContent[offset + l++];
                                        int repeatLen = tmpImgContent[offset + l];

                                        for (int r = 0; r < repeatLen; r++)
                                        {
                                            // 將解碼的位元加入tmpResult
                                            tmpResult.Add(repeatByte);
                                        }
                                    }
                                    else
                                    {
                                        // 將解碼的位元加入tmpResult
                                        tmpResult.Add(tmpImgContent[offset + l]);
                                    }
                                }
                            }

                            offset += tmpDataLen;
                        }
                    }

                    return tmpResult.ToArray();
                });
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 將HD-COPY製作的*.IMG檔案轉換為標準虛擬軟碟格式(*.vfd)檔案
        /// </summary>
        /// <param name="imgFile">HD-COPY映像檔案(IMG)的位元組陣列</param>
        /// <returns>標準虛擬軟碟(VFD)的位元組陣列。</returns>
        public static byte[] ConvertImgToVfd(byte[] imgFile)
        {
            return ConvertImgToVfdAsync(imgFile).GetAwaiter().GetResult();
        }
    }
}
