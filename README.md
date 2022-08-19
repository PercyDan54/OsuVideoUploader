# OsuVideoUploader
A tool to upload replays to Bilibili automatically

## Requirements
.NET 6 Runtime

[biliup-rs](https://github.com/ForgQi/biliup-rs) (assumes you already logged in and have cookies.json saved)

[danser-go](https://github.com/Wieku/danser-go) (assumes beatmap is present)

## Config
* `BiliupPath`: Path to [biliup-rs](https://github.com/ForgQi/biliup-rs) executable

* `DanserPath`: Path to [danser-go](https://github.com/Wieku/danser-go) executable

* `DanserArgs`: Custom arguments appended when running danser

* `VideoTags`: Tags for uploaded video
