using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HandyLime.Core;
namespace HandyLime.App;
public sealed partial class ToolWindow
{
    void ConfigureImage()
    {
        var id=operation.Id;
        if(id is "crop-image" or "redact-image"){ConfigureRegion(id=="redact-image");return;}
        if(id=="annotate"){
            canvas.Annotating=true;var tools=new WrapPanel();controls.Children.Add(tools);
            foreach(var tool in new[]{"select","arrow","line","rectangle","ellipse","text","pen"}){var button=new Button{Content=Icons.Label(tool switch{"select"=>"mouse-pointer-2","arrow"=>"arrow-up-right","line"=>"minus","rectangle"=>"rectangle-horizontal","ellipse"=>"circle","text"=>"type",_=>"pen-tool"},CultureInfo.InvariantCulture.TextInfo.ToTitleCase(tool),14),Padding=new Thickness(9,7,9,7),Margin=new Thickness(0,0,5,5),FontSize=11};button.Click+=(_,_)=>{canvas.DrawingTool=tool;status.Text=tool=="select"?"Drag an annotation to move it. Delete removes it.":"Draw on the image. Use Select to move an annotation.";};tools.Children.Add(button);}
            ColorControl("color","Stroke color","#B9E44D",color=>canvas.MarkColor=color);TextField("text","Text label","Your label",text=>canvas.MarkText=text);
            SliderField("fontSize","Text size",8,200,36,1,value=>canvas.FontSizeValue=value);SliderField("strokeWidth","Stroke width",1,30,4,1,value=>canvas.StrokeWidth=value);
            var row=new WrapPanel();controls.Children.Add(row);ActionButton(row,"Undo",()=>canvas.Undo());ActionButton(row,"Delete selected",()=>canvas.DeleteSelected());ActionButton(row,"Reset",()=>canvas.ResetMarks());return;
        }
        if(id=="adjust"){
            var compareBox=new CheckBox{Content="Show original (compare)"};compareBox.Click+=(_,_)=>{compare=compareBox.IsChecked==true;SchedulePreview();};controls.Children.Add(compareBox);
            Section("Light");SliderField("exposure","Exposure (EV)",-3,3,0,0.1);SliderField("brightness","Brightness",0,200,100,1);SliderField("contrast","Contrast",-100,100,0,1);
            SliderField("highlights","Highlights",-100,100,0,1);SliderField("shadows","Shadows",-100,100,0,1);SliderField("whites","Whites",-100,100,0,1);SliderField("blacks","Blacks",-100,100,0,1);SliderField("gamma","Gamma",0.2,3,1,0.05);
            Section("Color");SliderField("temperature","Warmth",-100,100,0,1);SliderField("tint","Tint",-100,100,0,1);SliderField("saturation","Saturation",0,200,100,1);SliderField("vibrance","Vibrance",-100,100,0,1);SliderField("hue","Hue",-180,180,0,1);Section("Detail");SliderField("sharpen","Sharpen",0,5,0,0.1);return;
        }
        if(id is "resize" or "background"){
            NumericField("width","Width (px)",originalWidth,1,16384);NumericField("height","Height (px)",originalHeight,1,16384);
            var row=new WrapPanel();controls.Children.Add(row);foreach(var factor in new[]{0.25,0.5,1.0,2.0})ActionButton(row,$"{factor*100:0}%",()=>{Set("width",Math.Clamp(Math.Round(originalWidth*factor),1,16384));Set("height",Math.Clamp(Math.Round(originalHeight*factor),1,16384));SchedulePreview();});if(id=="background")ColorControl("color","Background","#FFFFFF");
        }else if(id=="rotate"){SliderField("degrees","Clockwise rotation",-180,180,90,1);var row=new WrapPanel();controls.Children.Add(row);foreach(var angle in new[]{0,90,180,-90})ActionButton(row,$"{angle}°",()=>{Set("degrees",angle);SchedulePreview();});}
        else if(id=="collage"){SliderField("columns","Columns",1,Math.Min(10,inputs.Count),2,1);SliderField("cell","Cell size",64,2048,640,16);SliderField("gap","Spacing",0,200,16,1);ColorControl("color","Background","#FFFFFF");FileOrder();}
        else foreach(var field in operation.Fields){if(field.Key=="quality")SliderField("quality","Quality",1,100,85,1);else TextField(field.Key,field.Label,field.Default);}
        if(operation.Fields.Length==0)controls.Children.Add(new TextBlock{Text="The preview shows the copy that will be saved.",FontSize=12});
    }
    void ConfigureRegion(bool cover)
    {
        canvas.RegionEnabled=true;canvas.CoverRegion=cover;canvas.Region=cover?new Rect(canvas.DocumentWidth*0.25,canvas.DocumentHeight*0.25,canvas.DocumentWidth*0.5,canvas.DocumentHeight*0.5):new Rect(0,0,canvas.DocumentWidth,canvas.DocumentHeight);
        controls.Children.Add(new TextBlock{Text=cover?"Drag the black cover or resize its handles.":"Drag inside the crop to move it. Drag an orange handle to resize.",Margin=new Thickness(0,0,0,14)});
        if(!cover){Section("Aspect ratio");var row=new WrapPanel();controls.Children.Add(row);foreach(var pair in new[]{("Free",0d),("1:1",1d),("16:9",16d/9),("9:16",9d/16),("4:3",4d/3),("3:4",3d/4)})ActionButton(row,pair.Item1,()=>canvas.ApplyAspect(pair.Item2));}
        NumericField("width","Width (px)",canvas.Region.Width,1,canvas.DocumentWidth);NumericField("height","Height (px)",canvas.Region.Height,1,canvas.DocumentHeight);NumericField("x","Left (px)",canvas.Region.X,0,canvas.DocumentWidth-1);NumericField("y","Top (px)",canvas.Region.Y,0,canvas.DocumentHeight-1);
        var reset=new Button{Content="Reset rectangle",Margin=new Thickness(0,12,0,0)};reset.Click+=(_,_)=>{canvas.Region=new Rect(0,0,canvas.DocumentWidth,canvas.DocumentHeight);canvas.AspectRatio=0;UpdateRegionFields();};controls.Children.Add(reset);
    }
    void ConfigureMedia()
    {
        var lower=new StackPanel{Margin=new Thickness(0,12,0,0)};Grid.SetRow(lower,1);previewArea.Children.Add(lower);lower.Children.Add(timeline);
        var transport=new WrapPanel();lower.Children.Add(transport);play.Click+=(_,_)=>TogglePlayback();transport.Children.Add(play);ActionButton(transport,"Previous frame",()=>Seek(-1d/30));ActionButton(transport,"Next frame",()=>Seek(1d/30));
        if(timeline.SelectRange){ActionButton(transport,"Set start",()=>{timeline.Start=Math.Min(timeline.Position,timeline.End-0.05);UpdateTimeFields();timeline.InvalidateVisual();});ActionButton(transport,"Set end",()=>{timeline.End=Math.Max(timeline.Position,timeline.Start+0.05);UpdateTimeFields();timeline.InvalidateVisual();});}
        if(timeline.Splitting){ActionButton(transport,"Split here",()=>timeline.AddCut());ActionButton(transport,"Remove marker",()=>timeline.RemoveCut());}
        if(operation.Id is "crop-video" or "redact-video")ConfigureRegion(operation.Id=="redact-video");
        if(timeline.SelectRange){Section("Selected time range");NumericField("start","Start (seconds)",0,0,info!.Duration);NumericField("end","End (seconds)",info.Duration,0,info.Duration);}
        if(operation.Id=="frame")NumericField("start","Frame time (seconds)",0,0,info!.Duration);
        if(timeline.Splitting){
            controls.Children.Add(new TextBlock{Text="Move the playhead, choose Split here, then drag the numbered markers to refine each cut.",Margin=new Thickness(0,0,0,12)});NumericField("seconds","Equal clip length (seconds)",Math.Min(30,Math.Max(1,info!.Duration/2)),1,86400);
            var equal=new Button{Content="Add equally spaced markers",Margin=new Thickness(0,8,0,8)};equal.Click+=(_,_)=>{try{var length=Values.Number(values,"seconds",30,1,86400);timeline.Cuts.Clear();for(var t=length;t<info.Duration-0.05;t+=length){if(timeline.Cuts.Count>=999)break;timeline.Cuts.Add(t);}timeline.InvalidateVisual();UpdateSummary();}catch(Exception ex){Error(ex.Message);}};controls.Children.Add(equal);timeline.Position=info.Duration/2;timeline.AddCut();
        }
        if(operation.Id=="compress-video")SliderField("crf","Compression · smaller →",18,40,28,1);
        if(operation.Id=="speed")SliderField("speed","Playback speed",0.25,4,2,0.25);
        if(operation.Id=="channels"){var row=new WrapPanel();controls.Children.Add(row);ActionButton(row,"Mono",()=>Set("channels",1));ActionButton(row,"Stereo",()=>Set("channels",2));values["channels"]="1";}
        if(operation.Id=="convert:gif"){Section("Animated GIF");SliderField("fps","Frames per second",1,30,15,1);SliderField("width","Width (px)",64,1920,Math.Min(640,Math.Max(64,info!.Width)),16);controls.Children.Add(new TextBlock{Text="Select a short interval for a smaller GIF. GIF has no audio.",FontSize=12,Margin=new Thickness(0,8,0,12)});}
        
        if(operation.Id is "crop-video" or "redact-video" or "speed" or "compress-video" or "convert:gif" or "bleep" or "normalize" or "channels"){
            var preview=new Button{Content=Icons.Label("play","Preview result · 5 seconds"),Margin=new Thickness(0,18,0,0)};preview.Click+=async(_,_)=>{preview.IsEnabled=false;try{outputPreviewTask=PreviewResult();await outputPreviewTask;}finally{if(!lifetime.IsCancellationRequested)preview.IsEnabled=true;}};controls.Children.Add(preview);
        }
    }
    void ConfigureFiles(){previewArea.Children.Clear();var stack=new StackPanel{Margin=new Thickness(20)};stack.Children.Add(new TextBlock{Text=operation.Title,FontSize=24,FontWeight=FontWeights.SemiBold});stack.Children.Add(new TextBlock{Text=operation.Description,Margin=new Thickness(0,14,0,20)});foreach(var path in inputs)stack.Children.Add(new TextBlock{Text="▧  "+Path.GetFileName(path),Margin=new Thickness(0,8,0,8)});previewArea.Children.Add(new ScrollViewer{Content=stack});FileOrder();foreach(var field in operation.Fields)TextField(field.Key,field.Label,field.Default);}
    async Task ConfigurePdf()
    {
        previewArea.Children.Clear();pdfList=new ListBox{HorizontalContentAlignment=HorizontalAlignment.Stretch,BorderThickness=new Thickness(0),Background=Brushes.Transparent};previewArea.Children.Add(pdfList);
        if(operation.Id=="pdf-merge"){FileOrder();foreach(var path in inputs){var bitmap=PreviewBitmap.FromBytes(await PreviewEngine.PdfPage(path,0,lifetime.Token));var row=new StackPanel();row.Children.Add(new Image{Source=bitmap,Height=170,Stretch=Stretch.Uniform});row.Children.Add(new TextBlock{Text=Path.GetFileName(path),TextAlignment=TextAlignment.Center});pdfList.Items.Add(new ListBoxItem{Content=row,Tag=path});}return;}
        using var pdf=PdfSharp.Pdf.IO.PdfReader.Open(inputs[0],PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);var count=pdf.PageCount;if(count>300)throw new InvalidDataException("The visual page editor supports up to 300 pages per document.");
        for(var i=0;i<count;i++){lifetime.Token.ThrowIfCancellationRequested();status.Text=$"Loading page {i+1}/{count}…";var picture=PreviewBitmap.FromBytes(await PreviewEngine.PdfPage(inputs[0],i,lifetime.Token));pdfList.Items.Add(PageRow(new(i,0,picture)));}pdfList.SelectedIndex=0;
        if(operation.Id=="pdf-pages"){
            Section("Page order");controls.Children.Add(new TextBlock{Text="Select a page, then move, rotate, duplicate or remove it. Every thumbnail represents one output page.",Margin=new Thickness(0,0,0,12)});var row=new WrapPanel();controls.Children.Add(row);ActionButton(row,"Move up",()=>MovePage(-1));ActionButton(row,"Move down",()=>MovePage(1));
            ActionButton(row,"Rotate 90°",()=>{if(pdfList.SelectedItem is ListBoxItem selected){var index=pdfList.SelectedIndex;var page=(PageItem)selected.Tag;pdfList.Items[index]=PageRow(page with{Rotation=(page.Rotation+90)%360});pdfList.SelectedIndex=index;}});
            ActionButton(row,"Duplicate",()=>{if(pdfList.SelectedItem is ListBoxItem selected)pdfList.Items.Insert(pdfList.SelectedIndex+1,PageRow((PageItem)selected.Tag));});ActionButton(row,"Remove page",()=>{if(pdfList.SelectedIndex>=0)pdfList.Items.RemoveAt(pdfList.SelectedIndex);});
        }else controls.Children.Add(new TextBlock{Text=$"{count} pages · every page will be included.",Margin=new Thickness(0,12,0,0)});
    }
    ListBoxItem PageRow(PageItem item){var row=new DockPanel{Margin=new Thickness(10)};row.Children.Add(new Image{Source=item.Picture,Width=150,Height=200,Stretch=Stretch.Uniform,LayoutTransform=new RotateTransform(item.Rotation),Margin=new Thickness(0,0,15,0)});row.Children.Add(new TextBlock{Text=$"Page {item.Index+1} · {item.Rotation}°",VerticalAlignment=VerticalAlignment.Center});return new ListBoxItem{Content=row,Tag=item};}
    void MovePage(int delta){var index=pdfList!.SelectedIndex;var next=index+delta;if(index<0||next<0||next>=pdfList.Items.Count)return;var item=pdfList.Items[index];pdfList.Items.RemoveAt(index);pdfList.Items.Insert(next,item);pdfList.SelectedIndex=next;pdfList.ScrollIntoView(item);}
    void FileOrder()
    {
        var list=new ListBox{MaxHeight=180,Margin=new Thickness(0,8,0,8)};foreach(var path in inputs)list.Items.Add(Path.GetFileName(path));controls.Children.Add(list);var row=new WrapPanel();controls.Children.Add(row);
        foreach(var delta in new[]{-1,1})ActionButton(row,delta<0?"Move up":"Move down",()=>{var index=list.SelectedIndex;var next=index+delta;if(index<0||next<0||next>=inputs.Count)return;(inputs[index],inputs[next])=(inputs[next],inputs[index]);var item=list.Items[index];list.Items.RemoveAt(index);list.Items.Insert(next,item);list.SelectedIndex=next;if(operation.Id=="pdf-merge"&&pdfList!=null){var pages=pdfList.Items.Cast<ListBoxItem>().ToArray();pdfList.Items.Clear();foreach(var path in inputs)pdfList.Items.Add(pages.Single(p=>(string)p.Tag==path));}SchedulePreview();});
    }
}


