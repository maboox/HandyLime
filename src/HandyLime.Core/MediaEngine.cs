using System.Text.Json;

namespace HandyLime.Core;

public sealed class MediaEngine(Engines engines)
{
    public record Info(double Duration, bool HasAudio, int Width, int Height);
    public async Task<Info> Probe(string input, CancellationToken token)
    {
        var text = await ProcessRunner.Run(engines.Require("FFprobe"),["-v","error","-protocol_whitelist","file,pipe","-show_format","-show_streams","-of","json",input],token);
        using var doc = JsonDocument.Parse(text);
        var duration = 0d; var audio = false; int width = 0, height = 0;
        if (doc.RootElement.TryGetProperty("format",out var format) && format.TryGetProperty("duration",out var dur)) double.TryParse(dur.GetString(),System.Globalization.CultureInfo.InvariantCulture,out duration);
        foreach (var stream in doc.RootElement.GetProperty("streams").EnumerateArray()) {
            var type = stream.GetProperty("codec_type").GetString(); if (type == "audio") audio = true;
            if (type == "video" && width==0) {
                if (stream.TryGetProperty("width",out var w)) width = w.GetInt32(); if (stream.TryGetProperty("height",out var h)) height = h.GetInt32();
                if (stream.TryGetProperty("side_data_list",out var data)) foreach (var entry in data.EnumerateArray())
                    if (entry.TryGetProperty("rotation",out var rotation) && Math.Abs(rotation.GetDouble())%180>45) (width,height)=(height,width);
            }
        }
        return new(duration,audio,width,height);
    }
    sealed class MediaProgress(IProgress<JobProgress>? target,double duration,double offset,double span,string label) : IProgress<string>
    {
        public void Report(string value) {
            if (value.StartsWith("Processed ") && TimeSpan.TryParse(value[10..],System.Globalization.CultureInfo.InvariantCulture,out var time)) {
                var fraction=duration>0?Math.Clamp(time.TotalSeconds/duration,0,0.995):0;
                target?.Report(new($"{label} · {time:mm\\:ss} processed",duration>0?Math.Min(0.99,offset+fraction*span):null));
            }
        }
    }
    async Task Run(IEnumerable<string> args, CancellationToken token, IProgress<JobProgress>? progress, double duration=0,double offset=0,double span=1,string label="Converting",string? workingDirectory=null) =>
        await ProcessRunner.Run(engines.Require("FFmpeg"),new[] { "-hide_banner","-nostdin","-n","-loglevel","error","-progress","pipe:1","-stats_period","0.2" }.Concat(args),token,new MediaProgress(progress,duration,offset,span,label),workingDirectory);
    static string[] Input(string path) => ["-protocol_whitelist","file,pipe","-i",path];
    static string[] AudioCodec(string ext) => ext switch {
        "mp3" => ["-c:a","libmp3lame","-q:a","2"], "m4a" => ["-c:a","aac","-b:a","192k"],
        "wav" => ["-c:a","pcm_s16le"], "flac" => ["-c:a","flac"], "ogg" => ["-c:a","libvorbis","-q:a","5"],
        "opus" => ["-c:a","libopus","-b:a","128k"], "aiff" => ["-c:a","pcm_s16be"],
        "wma" => ["-c:a","wmav2","-b:a","192k"], _ => ["-c:a","aac","-b:a","192k"]
    };
    static string[] VideoCodec(string ext, string crf = "23") => ext switch {
        "webm" => ["-c:v","libvpx-vp9","-crf",crf,"-b:v","0","-c:a","libopus"],
        "avi" => ["-c:v","mpeg4","-q:v","4","-c:a","libmp3lame"],
        "wmv" => ["-c:v","wmv2","-b:v","2500k","-c:a","wmav2"],
        _ => ["-c:v","libx264","-preset","medium","-crf",crf,"-pix_fmt","yuv420p","-c:a","aac","-b:a","192k"]
    };
    public async Task Process(string[] inputs, string id, Dictionary<string,string> options, string output, CancellationToken token, IProgress<JobProgress>? progress)
    {
        if (id == "join-video") { await Join(inputs,output,token,progress); return; }
        var input = inputs[0]; progress?.Report(new("Reading media information…"));
        var info = await Probe(input,token); var ext = Path.GetExtension(output).TrimStart('.');
        if (id == "adjust-video") { await AdjustVideo(input, options, output, info, token, progress); return; }
        var expectedDuration=info.Duration;
        double N(string key,double fallback,double min,double max) => Values.Number(options,key,fallback,min,max);
        string S(double value) => Values.Invariant(value);
        var args = new List<string>(Input(input));
        var videoFilter = "pad=ceil(iw/2)*2:ceil(ih/2)*2";
        string Rectangle() {
            var x = Values.Integer(options,"x",0,0,32768); var y = Values.Integer(options,"y",0,0,32768);
            var w = Values.Integer(options,"width",1280,1,16384); var h = Values.Integer(options,"height",720,1,16384);
            if (x+w > info.Width || y+h > info.Height) throw new ArgumentException($"Rectangle exceeds video dimensions ({info.Width}×{info.Height}).");
            return $"{w}:{h}:{x}:{y}";
        }
        (double start,double end) Interval() {
            var start = N("start",0,0,864000); var end = N("end",10,0,864000);
            if (end <= start || (info.Duration > 0 && end > info.Duration + 0.05)) throw new ArgumentException($"Time interval is invalid. File duration: {S(info.Duration)} seconds.");
            return (start,end);
        }
        if (id == "split-video") {
            var seconds = N("seconds",30,1,86400);
            var cuts = Values.Text(options,"cuts");
            if (info.Duration <= 0 || cuts.Length==0 && info.Duration / seconds > 1000) throw new ArgumentException("Unknown duration or more than 1,000 clips requested.");
            var boundaries = cuts.Length>0 ? Editing.SplitPoints(cuts,info.Duration) : Enumerable.Range(0,(int)Math.Ceiling(info.Duration/seconds)).Select(i=>i*seconds).Append(info.Duration).ToArray();
            Directory.CreateDirectory(output);
            for (int i=0; i<boundaries.Length-1; i++) {
                token.ThrowIfCancellationRequested();
                var length=boundaries[i+1]-boundaries[i];
                await Run(new[]{"-ss",S(boundaries[i])}.Concat(Input(input)).Concat(new[] { "-t",S(length),"-map","0:v:0","-map","0:a:0?","-vf",videoFilter }).Concat(VideoCodec("mp4")).Append(Path.Combine(output,$"clip-{i+1:0000}.mp4")),token,progress,length,(double)i/(boundaries.Length-1),1d/(boundaries.Length-1),$"Saving clip {i+1}/{boundaries.Length-1}");
            }
            return;
        }
        if (id == "frame") {
            var start = N("start",0,0,864000); if (info.Duration > 0 && start >= info.Duration) throw new ArgumentException("Frame time must be before the end of the video.");
            args.AddRange(["-ss",S(start),"-frames:v","1","-update","1",output]); await Run(args,token,progress);
            if((!File.Exists(output)||new FileInfo(output).Length==0)&&start>=Math.Max(0,info.Duration-0.5)){if(File.Exists(output))File.Delete(output);await PreviewEngine.LastFrame(engines,input,output,token,false);}return;
        }
        if (id == "strip-media") {
            args.AddRange(["-map","0:v?","-map","0:a?","-map_metadata","-1","-map_metadata:s","-1","-map_chapters","-1","-c","copy",output]); await Run(args,token,progress); return;
        }
        if (id == "visualizer") {
            args.AddRange(["-filter_complex","[0:a]showwaves=s=1280x720:mode=line:colors=0xB9E44D:rate=30,format=yuv420p[v]","-map","[v]","-map","0:a:0"]);
            args.AddRange(VideoCodec("mp4")); args.AddRange(["-shortest",output]); await Run(args,token,progress); return;
        }
        if (id == "bleep") {
            var (start,end) = Interval();
            if (info.Duration <= 0) throw new ArgumentException("Audio duration is unavailable.");
            args.AddRange(["-f","lavfi","-i",$"sine=frequency=1000:duration={S(info.Duration)}",
                "-filter_complex",$"[0:a]volume=0:enable='between(t,{S(start)},{S(end)})'[clean];[1:a]volume=0.25,volume=0:enable='not(between(t,{S(start)},{S(end)}))'[tone];[clean][tone]amix=inputs=2:duration=first:normalize=0[a]","-map","[a]","-c:a","pcm_s16le",output]);
            await Run(args,token,progress); return;
        }
        var audioOutput = new[] {"mp3","m4a","wav","flac","ogg","opus","aiff","wma"}.Contains(ext);
        if (audioOutput) {
            if (!info.HasAudio) throw new ArgumentException("The input contains no audio track.");
            args.AddRange(["-map","0:a:0","-vn"]);
            if (id == "trim-audio") { var (start,end) = Interval(); args.AddRange(["-ss",S(start),"-t",S(end-start)]); }
            if (id == "normalize") args.AddRange(["-af","loudnorm=I=-16:TP=-1.5:LRA=11"]);
            if (id == "channels") args.AddRange(["-ac",Values.Integer(options,"channels",1,1,2).ToString()]);
            if (id == "strip-audio") args.AddRange(["-map_metadata","-1","-map_metadata:s","-1"]);
            if (id=="trim-audio") { var (start,end)=Interval(); expectedDuration=end-start; }
            args.AddRange(AudioCodec(ext)); args.Add(output); await Run(args,token,progress,expectedDuration); return;
        }
        if (ext == "gif") {
            var fps=Values.Integer(options,"fps",15,1,30); var width=Values.Integer(options,"width",640,64,1920);
            var start=N("start",0,0,Math.Max(info.Duration,0)); var end=N("end",info.Duration,0,Math.Max(info.Duration,0));
            if (end<=start) throw new ArgumentException("Choose a non-empty GIF interval.");
            expectedDuration=end-start;
            // Trim before palette generation; otherwise the palette pass decodes the entire source.
            args = ["-ss",S(start)]; args.AddRange(Input(input));
            args.AddRange(["-t",S(expectedDuration),"-filter_complex",$"[0:v:0]trim=duration={S(expectedDuration)},setpts=PTS-STARTPTS,fps={fps},scale={width}:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff[p];[b][p]paletteuse=dither=sierra2_4a","-an","-loop","0",output]);
            progress?.Report(new("Building GIF palette…",0)); await Run(args,token,progress,expectedDuration); return;
        }
        args.AddRange(["-map","0:v:0","-map","0:a:0?"]);
        if (id == "trim-video") { var (start,end) = Interval(); args.AddRange(["-ss",S(start),"-t",S(end-start)]); }
        if (id == "crop-video") videoFilter = "crop="+Rectangle()+":exact=1,"+videoFilter;
        if (id == "redact-video") {
            var rect = Rectangle().Split(':'); var (start,end) = Interval();
            videoFilter = $"drawbox=x={rect[2]}:y={rect[3]}:w={rect[0]}:h={rect[1]}:color=black:t=fill:enable='between(t,{S(start)},{S(end)})',"+videoFilter;
        }
        if (id == "speed") {
            var speed = N("speed",2,0.25,4); videoFilter = $"setpts=PTS/{S(speed)},"+videoFilter;
            if (info.HasAudio) {
                var filters = new List<string>(); var factor = speed;
                while (factor > 2) { filters.Add("atempo=2"); factor/=2; }
                while (factor < 0.5) { filters.Add("atempo=0.5"); factor/=0.5; }
                filters.Add("atempo="+S(factor)); args.AddRange(["-af",string.Join(',',filters)]);
            }
        }
        args.AddRange(["-vf",videoFilter]); args.AddRange(VideoCodec(ext,id == "compress-video" ? S(N("crf",28,18,40)) : "23"));
        if (ext is "mp4" or "mov") args.AddRange(["-movflags","+faststart"]);
        if (id=="trim-video") {var (start,end)=Interval();expectedDuration=end-start;}
        if (id=="speed") expectedDuration/=N("speed",2,0.25,4);
        args.Add(output); await Run(args,token,progress,expectedDuration);
    }
    async Task AdjustVideo(string input,Dictionary<string,string> options,string output,Info info,CancellationToken token,IProgress<JobProgress>? progress)
    {
        ColorAdjustments.Validate(options);
        if (info.Width <= 0 || info.Height <= 0) throw new ArgumentException("The input contains no video track.");
        var work = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, "grade-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try {
            progress?.Report(new("Preparing light and color adjustments…", 0));
            await Task.Run(() => VideoAdjustments.WriteCube(Path.Combine(work,"colors.cube"), options, token), token);
            // Use a fixed relative LUT name to avoid FFmpeg filter-path escaping.
            var filter = "format=rgb24,lut3d=file=colors.cube:interp=tetrahedral";
            var sharpen = Values.Number(options,"sharpen",0,0,5);
            if (sharpen > 0) filter += ",unsharp=5:5:" + Values.Invariant(sharpen / 2) + ":5:5:0";
            filter += ",pad=ceil(iw/2)*2:ceil(ih/2)*2";
            var args = new List<string>(Input(Path.GetFullPath(input)));
            args.AddRange(["-map","0:v:0","-map","0:a:0?","-vf",filter]);
            args.AddRange(VideoCodec("mp4","18"));
            args.AddRange(["-movflags","+faststart",Path.GetFullPath(output)]);
            await Run(args,token,progress,info.Duration,label:"Applying light and color",workingDirectory:work);
        } finally {
            try { Directory.Delete(work,true); } catch (IOException) {} catch (UnauthorizedAccessException) {}
        }
    }
    async Task Join(string[] inputs,string output,CancellationToken token,IProgress<JobProgress>? progress)
    {
        if (inputs.Length is < 2 or > 100) throw new ArgumentException("Select 2–100 videos to join.");
        var temp = Path.Combine(Path.GetDirectoryName(output)!,"join"); Directory.CreateDirectory(temp);
        var names = new List<string>();
        for (var i=0;i<inputs.Length;i++) {
            var info = await Probe(inputs[i],token); var name = $"part-{i:0000}.mp4"; names.Add(name);
            var args = new List<string>(Input(inputs[i]));
            if (!info.HasAudio) args.AddRange(["-f","lavfi","-i","anullsrc=channel_layout=stereo:sample_rate=48000"]);
            args.AddRange(["-map","0:v:0","-map",info.HasAudio ? "0:a:0" : "1:a:0","-vf","scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2,setsar=1,fps=30","-ar","48000","-ac","2"]);
            args.AddRange(VideoCodec("mp4")); args.AddRange(["-shortest",Path.Combine(temp,name)]); await Run(args,token,progress,info.Duration,(double)i/(inputs.Length+1),1d/(inputs.Length+1),$"Preparing clip {i+1}/{inputs.Length}");
        }
        var list = Path.Combine(temp,"list.txt"); await File.WriteAllLinesAsync(list,names.Select(n=>$"file '{n}'"),token);
        await Run(["-f","concat","-safe","1","-protocol_whitelist","file,pipe","-i",list,"-c","copy","-movflags","+faststart",output],token,progress);
    }
}
