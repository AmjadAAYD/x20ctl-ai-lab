using System.Buffers.Binary;
namespace X20Ctl.Zone.Screen;

/// <summary>Independent format implementation. Inputs are already decoded/cropped RGBA pixels.</summary>
public static partial class ScreenCodec
{
 public const int Width=160,Height=80,InputLimit=64*1024*1024,AssetLimit=3*1024*1024;
 // DIYTool allows 3 MiB containers, but X20 Pro (LT165 / 0x5E4017) crashes on nb > 1 blocks (even at 266,942 B) and crashed on a 518,154 B mode=1 / 160-frame upload; 467,042 B (35 frames, nb=1, mode=0) is hardware-verified.
 public const int HardwareAssetLimit=467_042;
 public static uint Crc(ReadOnlySpan<byte> data)
 {
  uint crc=0;foreach(byte b in data){crc^=b;for(int bit=0;bit<8;bit++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);}return crc;
 }
 static ushort[] Words(byte[] rgba,int width,int height)
 {
  if(width<1||height<1||width>65535||height>65535||rgba.Length!=checked(width*height*4))throw new ArgumentException("Expected decoded RGBA pixels.");
  var words=new ushort[width*height];int n=0;
  for(int y=0;y<width;y++)for(int x=0;x<height;x++){int i=(x*width+width-1-y)*4;words[n++]=(ushort)(((rgba[i]&248)<<8)|((rgba[i+1]&252)<<3)|(rgba[i+2]>>3));}
  return words;
 }
 public static byte[] EncodeStill(byte[] rgba,int width=Width,int height=Height)
 {
  var words=Words(rgba,width,height);int length=checked(words.Length*2);if(length+12>AssetLimit)throw new ArgumentException("Image exceeds 3 MiB.");
  var result=new byte[length+12];Put32(result,0,12);Put16(result,4,height);Put16(result,6,width);Put24(result,8,length);
  for(int i=0;i<words.Length;i++)BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(12+i*2),words[i]);return result;
 }
 public static byte[] EncodeAnimation(IReadOnlyList<byte[]> frames,int width=Width,int height=Height,bool truncateToLimit=false,CancellationToken cancellationToken=default,Action<string>? diagnostic=null)
 {
  if(frames.Count is <1 or >65535)throw new ArgumentException("Animation must contain 1 to 65535 frames.");
  var parts=new List<(byte[] Data,byte Kind)>();int payloadSize=0;
  foreach(var frame in frames){cancellationToken.ThrowIfCancellationRequested();var part=Compress(Words(frame,width,height),height,width,diagnostic);if(truncateToLimit&&2+16*parts.Count+payloadSize+part.Data.Length>AssetLimit)break;parts.Add(part);payloadSize=checked(payloadSize+part.Data.Length);if(!truncateToLimit&&2+16*parts.Count+payloadSize>AssetLimit)throw new ArgumentException("Animation exceeds 3 MiB. Reduce its frames.");}
  if(parts.Count==0)throw new ArgumentException("No animation frame fits.");
  return Assemble(parts,width,height);
 }
 static byte[] Assemble(List<(byte[] Data,byte Kind)> parts,int width,int height)
 {
  int payloadSize=parts.Sum(p=>p.Data.Length),offset=2+parts.Count*16+payloadSize;
  var result=new byte[offset];Put16(result,0,parts.Count);offset=2+parts.Count*16;
  for(int i=0;i<parts.Count;i++){int row=2+i*16;var part=parts[i];Put32(result,row,(uint)offset);Put16(result,row+8,height);Put16(result,row+10,width);Put24(result,row+12,part.Data.Length);result[row+15]=part.Kind;part.Data.CopyTo(result,offset);offset+=part.Data.Length;}return result;
 }
 /// <summary>The whole animation when it fits <paramref name="maxAssetBytes"/> (defaulting to the hardware-verified X20 Pro ceiling);
 /// otherwise every second, third, … frame (the first is always kept), with the delay stretched by the same step so the loop keeps its speed and length.
 /// Frames are quantized to &lt;= 255 RGB565 colors (and use non-RLE mode=0 for hardware-capped targets) so X20 Pro firmware never receives multi-block or unverified RLE streams.</summary>
 public static byte[] EncodeAnimationFitting(IReadOnlyList<byte[]> frames,int delayMs,out int used,out int delayUsed,int width=Width,int height=Height,CancellationToken cancellationToken=default,int maxAssetBytes=HardwareAssetLimit)
 {
  if(frames.Count is <1 or >65535)throw new ArgumentException("Animation must contain 1 to 65535 frames.");
  if(maxAssetBytes is <1 or >AssetLimit)throw new ArgumentOutOfRangeException(nameof(maxAssetBytes));
  bool allowRle=maxAssetBytes>HardwareAssetLimit;
  var compressed=new (byte[] Data,byte Kind)[frames.Count];
  foreach(var i in Enumerable.Range(0,frames.Count)){cancellationToken.ThrowIfCancellationRequested();compressed[i]=Compress(QuantizeTo255Colors(Words(frames[i],width,height)),height,width,null,allowRle);}
  for(int step=1;step<=frames.Count;step++)
  {
   var kept=Enumerable.Range(0,(frames.Count+step-1)/step).Select(n=>compressed[n*step]).ToList();
   if(2+16*kept.Count+kept.Sum(p=>p.Data.Length)>maxAssetBytes)continue;
   used=kept.Count;delayUsed=Math.Clamp(delayMs*step,10,2550);return Assemble(kept,width,height);
  }
  throw new ArgumentException("Not even one frame fits. Use a simpler image.");
 }
 static readonly IComparer<(ushort Word,int Weight)>[] ChannelComparers=
 [
  Comparer<(ushort Word,int Weight)>.Create((a,b)=>((a.Word>>8)&0xf8).CompareTo((b.Word>>8)&0xf8)),
  Comparer<(ushort Word,int Weight)>.Create((a,b)=>((a.Word>>3)&0xfc).CompareTo((b.Word>>3)&0xfc)),
  Comparer<(ushort Word,int Weight)>.Create((a,b)=>(a.Word&0x1f).CompareTo(b.Word&0x1f)),
 ];
 // Compress breaks into block 2 as soon as palette.Count reaches 256, so single-block hardware frames require <= 255 distinct RGB565 colors.
 internal static ushort[] QuantizeTo255Colors(ushort[] words)
 {
  var counts=new Dictionary<ushort,int>();
  foreach(var w in words)counts[w]=counts.TryGetValue(w,out int c)?c+1:1;
  if(counts.Count<=255)return words;
  var entries=counts.Select(kv=>(Word:kv.Key,Weight:kv.Value)).ToArray();
  (int Start,int Length,int Axis,int Spread,int Weight) Measure(int start,int length)
  {
   int minR=248,maxR=0,minG=252,maxG=0,minB=248,maxB=0,weight=0;
   for(int j=start;j<start+length;j++)
   {
    ushort w=entries[j].Word;weight+=entries[j].Weight;
    int r=(w>>8)&0xf8,g=(w>>3)&0xfc,b=(w&0x1f)<<3;
    if(r<minR)minR=r;if(r>maxR)maxR=r;if(g<minG)minG=g;if(g>maxG)maxG=g;if(b<minB)minB=b;if(b>maxB)maxB=b;
   }
   int rSpan=maxR-minR,gSpan=maxG-minG,bSpan=maxB-minB;
   int axis=gSpan>=rSpan&&gSpan>=bSpan?1:bSpan>=rSpan?2:0;
   return(start,length,axis,axis==1?gSpan:axis==2?bSpan:rSpan,weight);
  }
  var buckets=new List<(int Start,int Length,int Axis,int Spread,int Weight)>(255){Measure(0,entries.Length)};
  while(buckets.Count<255)
  {
   int bestIndex=-1,bestSpread=0,bestWeight=0;
   for(int i=0;i<buckets.Count;i++){var b=buckets[i];if(b.Length>1&&(b.Spread>bestSpread||(b.Spread==bestSpread&&b.Weight>bestWeight))){bestIndex=i;bestSpread=b.Spread;bestWeight=b.Weight;}}
   if(bestIndex<0||bestSpread==0)break;
   var chosen=buckets[bestIndex];
   Array.Sort(entries,chosen.Start,chosen.Length,ChannelComparers[chosen.Axis]);
   long half=chosen.Weight/2,acc=0;int split=chosen.Start+1;
   while(split<chosen.Start+chosen.Length-1){acc+=entries[split-1].Weight;if(acc>=half)break;split++;}
   buckets[bestIndex]=Measure(chosen.Start,split-chosen.Start);
   buckets.Add(Measure(split,chosen.Start+chosen.Length-split));
  }
  var palette=new ushort[buckets.Count];
  for(int i=0;i<buckets.Count;i++)
  {
   var (start,length,_,_,_)=buckets[i];long sr=0,sg=0,sb=0,tw=0;
   for(int j=start;j<start+length;j++){ushort w=entries[j].Word;int wt=entries[j].Weight;sr+=(long)((w>>11)&0x1f)*wt;sg+=(long)((w>>5)&0x3f)*wt;sb+=(long)(w&0x1f)*wt;tw+=wt;}
   int r5=(int)((sr*2+tw)/(2*tw)),g6=(int)((sg*2+tw)/(2*tw)),b5=(int)((sb*2+tw)/(2*tw));
   palette[i]=(ushort)((Math.Clamp(r5,0,31)<<11)|(Math.Clamp(g6,0,63)<<5)|Math.Clamp(b5,0,31));
  }
  var remap=new Dictionary<ushort,ushort>(entries.Length);
  foreach(var (w,_) in entries)
  {
   int r=(w>>8)&0xf8,g=(w>>3)&0xfc,b=(w&0x1f)<<3,bestD=int.MaxValue;ushort bestW=palette[0];
   foreach(ushort p in palette)
   {
    int dr=r-((p>>8)&0xf8),dg=g-((p>>3)&0xfc),db=b-((p&0x1f)<<3),d=dr*dr+dg*dg+db*db;
    if(d<bestD){bestD=d;bestW=p;if(d==0)break;}
   }
   remap[w]=bestW;
  }
  var quantized=new ushort[words.Length];
  for(int i=0;i<words.Length;i++)quantized[i]=remap[words[i]];
  return quantized;
 }
 public static void ValidateHardwareSafe(byte[] asset,bool animation)
 {
  if(asset.Length is <1 or >HardwareAssetLimit)
   throw new InvalidOperationException($"Screen asset ({asset.Length:N0} B) exceeds the hardware-verified safe limit ({HardwareAssetLimit:N0} B).");
  if(!animation)return;
  if(asset.Length<2)throw new InvalidOperationException("Screen animation directory is truncated.");
  int count=BinaryPrimitives.ReadUInt16LittleEndian(asset);
  if(count<1||asset.Length<2+count*16)throw new InvalidOperationException("Screen animation directory is invalid.");
  for(int i=0;i<count;i++)
  {
   int row=2+i*16;
   uint offset=BinaryPrimitives.ReadUInt32LittleEndian(asset.AsSpan(row));
   int length=asset[row+12]|(asset[row+13]<<8)|(asset[row+14]<<16);
   if(asset[row+15]!=3||length<12+516||(ulong)offset+(uint)length>(ulong)asset.Length||asset[offset]!=0x4c||asset[offset+1]!=0x54||asset[offset+7]!=3)
    throw new InvalidOperationException("Screen animation contains non-LT frame encoding not verified safe on X20 Pro hardware.");
   if(asset[offset+6]!=1)
    throw new InvalidOperationException("Screen animation requires multi-block encoding (nb > 1), which crashes X20 Pro hardware.");
   if(asset[offset+15]!=0)
    throw new InvalidOperationException("Screen animation uses RLE mode=1, which is not verified safe on X20 Pro hardware.");
  }
 }
 static (byte[] Data,byte Kind) Compress(ushort[] words,int width,int height,Action<string>? diagnostic,bool allowRle=true)
 {
  var raw=new byte[words.Length*2];for(int i=0;i<words.Length;i++)Put16(raw,i*2,words[i]);
  var blocks=new List<(ushort[] Palette,byte Mode,byte[] Stream)>();int start=0;
  while(start<words.Length)
  {
   var palette=new List<ushort>{words[start]};var lookup=new Dictionary<ushort,byte>{{words[start],0}};int end=start+1;
   while(end<words.Length){ushort value=words[end];if(!lookup.ContainsKey(value)){lookup[value]=(byte)palette.Count;palette.Add(value);if(palette.Count==256)break;}end++;}
   var indices=new byte[end-start];for(int i=start;i<end;i++)indices[i-start]=lookup[words[i]];
   byte mode=0;byte[] stream=indices;
   if(allowRle){var runs=new List<byte>();for(int i=0;i<indices.Length;){int n=1;while(n<256&&i+n<indices.Length&&indices[i+n]==indices[i])n++;runs.Add((byte)(n-1));runs.Add(indices[i]);i+=n;}if(runs.Count<indices.Length){mode=1;stream=runs.ToArray();}}
   blocks.Add((palette.ToArray(),mode,stream));start=end;if(blocks.Count>255)return(raw,0);
  }
  if(blocks.Sum(b=>b.Stream.Length)>=raw.Length*7/10)return(raw,0);
  int size=12+blocks.Count*516+blocks.Sum(b=>b.Stream.Length);if((size&1)!=0&&size>=raw.Length){diagnostic?.Invoke("Raw fallback: odd LT payload exceeds the raw buffer.");return(raw,0);}var result=new byte[(size+1)&~1];result[0]=0x4c;result[1]=0x54;Put16(result,2,width);Put16(result,4,height);result[6]=(byte)blocks.Count;result[7]=3;Put32(result,8,(uint)size);
  int row=12,offset=12+516*blocks.Count;foreach(var block in blocks){Put24(result,row,block.Stream.Length);result[row+3]=block.Mode;for(int i=0;i<block.Palette.Length;i++)Put16(result,row+4+i*2,block.Palette[i]);row+=516;block.Stream.CopyTo(result,offset);offset+=block.Stream.Length;}
  // Original compressor overwrites its raw buffer; odd alignment retains the raw byte at this offset.
  // Padding beyond the original raw allocation is not established by the captured cases.
  if((size&1)!=0&&size<raw.Length)result[size]=raw[size];
  return(result,3);
 }
 public static byte[] Metadata(byte[] asset,bool animation,int delayMs=100)
 {
  if(asset.Length is <1 or >AssetLimit)throw new ArgumentException("Invalid screen asset size.");
  var block=Enumerable.Repeat((byte)255,4096).ToArray();new byte[]{0x68,0x45,0x71,0x19}.CopyTo(block,0);Put32(block,4,animation?5u:3u);Put32(block,8,(uint)asset.Length);Put32(block,12,Crc(asset));Put32(block,16,Crc(block.AsSpan(0,16)));if(animation)block[20]=(byte)Math.Clamp(delayMs/10,1,255);return block;
 }
 internal static void Put16(byte[] b,int i,int n)=>BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(i),checked((ushort)n));
 internal static void Put24(byte[] b,int i,int n){if(n<0||n>0xffffff)throw new ArgumentOutOfRangeException(nameof(n));b[i]=(byte)n;b[i+1]=(byte)(n>>8);b[i+2]=(byte)(n>>16);}
 internal static void Put32(byte[] b,int i,uint n)=>BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(i),n);
}
