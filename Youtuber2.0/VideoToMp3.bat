for %%f in (%1*.mp4) do ffmpeg -i "%%f" -f mp3 -ar 44100 "%2\%%~nf.mp3"

for %%f in (%1*.webm) do ffmpeg -i "%%f" -f mp3 -ar 44100 "%2\%%~nf.mp3"