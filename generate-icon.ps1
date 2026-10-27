$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$frames = @()
foreach ($size in @(16,20,24,32,48,64,128,256)) {
    $bitmap = [Drawing.Bitmap]::new($size,$size)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([Drawing.Color]::FromArgb(83,217,176))
    $graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(16,35,30))
    $font = [Drawing.Font]::new('Segoe UI', [single]($size*.94),[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
    $small = [Drawing.Font]::new('Segoe UI',[single]($size*.47),[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel)
    $graphics.DrawString('T',$font,$brush,[single](-$size*.11),[single](-$size*.14),[Drawing.StringFormat]::GenericTypographic)
    $graphics.DrawString('4',$small,$brush,[single]($size*.61),[single](-$size*.055),[Drawing.StringFormat]::GenericTypographic)
    $stream = [IO.MemoryStream]::new(); $bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png)
    $frames += ,@($size,$stream.ToArray())
    $stream.Dispose();$font.Dispose();$small.Dispose();$brush.Dispose();$graphics.Dispose();$bitmap.Dispose()
}
$output = [IO.File]::Create((Join-Path $PSScriptRoot 'src/TimersApp.Desktop/Resources/T4.ico'))
$writer = [IO.BinaryWriter]::new($output)
try {
    $writer.Write([uint16]0);$writer.Write([uint16]1);$writer.Write([uint16]$frames.Count)
    $offset = 6+16*$frames.Count
    foreach ($frame in $frames) {
        $dimension = if($frame[0] -eq 256){0}else{$frame[0]}
        $writer.Write([byte]$dimension);$writer.Write([byte]$dimension);$writer.Write([byte]0);$writer.Write([byte]0)
        $writer.Write([uint16]1);$writer.Write([uint16]32);$writer.Write([uint32]$frame[1].Length);$writer.Write([uint32]$offset);$offset += $frame[1].Length
    }
    foreach ($frame in $frames) {$writer.Write([byte[]]$frame[1])}
} finally {$writer.Dispose();$output.Dispose()}
