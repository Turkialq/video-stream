/* eslint-disable */
import React, {
  useState,
  useRef,
  useEffect,
  MouseEvent as ReactMouseEvent,
} from "react";
import "../VideoPlayer.css";

/**
 * Full custom video player with TypeScript.
 *
 * Assumptions:
 * - .NET backend at http://localhost:5284, serving:
 *   - /video   => chunked streaming
 *   - /assets/previewImgs/previewX.jpg => timeline preview images
 *   - /assets/subtitles.vtt => captions
 */
const VideoPlayer: React.FC = () => {
  // --------------------
  // Refs to DOM elements
  // --------------------
  const videoContainerRef = useRef<HTMLDivElement | null>(null);
  const videoRef = useRef<HTMLVideoElement | null>(null);
  const timelineContainerRef = useRef<HTMLDivElement | null>(null);
  const previewImgRef = useRef<HTMLImageElement | null>(null);
  const thumbnailImgRef = useRef<HTMLImageElement | null>(null);

  // --------------------
  // State
  // --------------------
  const [isScrubbing, setIsScrubbing] = useState(false);
  const [wasPaused, setWasPaused] = useState(true);

  const [volume, setVolume] = useState(1);
  const [muted, setMuted] = useState(false);

  const [captionsActive, setCaptionsActive] = useState(false);

  const [playbackRate, setPlaybackRate] = useState(1);

  const [isPaused, setIsPaused] = useState(true);
  const [isTheater, setIsTheater] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [isMiniPlayer, setIsMiniPlayer] = useState(false);

  // --------------------
  // Lifecycle / Effects
  // --------------------
  useEffect(() => {
    const video = videoRef.current;
    if (!video) return;

    // Handle play/pause
    const handlePlay = () => setIsPaused(false);
    const handlePause = () => setIsPaused(true);

    video.addEventListener("play", handlePlay);
    video.addEventListener("pause", handlePause);

    // Update timeline when currentTime changes
    const handleTimeUpdate = () => {
      if (!videoRef.current || !timelineContainerRef.current) return;
      const percent = videoRef.current.currentTime / videoRef.current.duration;
      timelineContainerRef.current.style.setProperty(
        "--progress-position",
        `${percent}`
      );
    };
    video.addEventListener("timeupdate", handleTimeUpdate);

    // On loadeddata => you could measure total time
    const handleLoadedData = () => {
      // e.g., measure video.duration, display in UI, etc.
    };
    video.addEventListener("loadeddata", handleLoadedData);

    // Volume change => update data-volume-level attribute
    const handleVolumeChange = () => {
      if (!videoContainerRef.current || !videoRef.current) return;
      if (videoRef.current.muted || videoRef.current.volume === 0) {
        videoContainerRef.current.setAttribute("data-volume-level", "muted");
      } else if (videoRef.current.volume >= 0.5) {
        videoContainerRef.current.setAttribute("data-volume-level", "high");
      } else {
        videoContainerRef.current.setAttribute("data-volume-level", "low");
      }
    };
    video.addEventListener("volumechange", handleVolumeChange);

    // Fullscreen changes
    const handleFullscreenChange = () => {
      setIsFullscreen(!!document.fullscreenElement);
    };
    document.addEventListener("fullscreenchange", handleFullscreenChange);

    // Picture-in-picture
    const handleEnterPiP = () => setIsMiniPlayer(true);
    const handleLeavePiP = () => setIsMiniPlayer(false);

    video.addEventListener("enterpictureinpicture", handleEnterPiP);
    video.addEventListener("leavepictureinpicture", handleLeavePiP);

    // Keyboard shortcuts
    const handleKeyDown = (e: KeyboardEvent) => {
      const tagName = (document.activeElement?.tagName || "").toLowerCase();
      if (tagName === "input") return;

      switch (e.key.toLowerCase()) {
        case " ":
          if (tagName === "button") return;
          // falls through
        case "k":
          togglePlay();
          break;
        case "f":
          toggleFullScreenMode();
          break;
        case "t":
          toggleTheaterMode();
          break;
        case "i":
          toggleMiniPlayerMode();
          break;
        case "m":
          toggleMute();
          break;
        case "arrowleft":
        case "j":
          skip(-5);
          break;
        case "arrowright":
        case "l":
          skip(5);
          break;
        case "c":
          toggleCaptions();
          break;
        default:
          break;
      }
    };
    document.addEventListener("keydown", handleKeyDown);

    // Cleanup
    return () => {
      video.removeEventListener("play", handlePlay);
      video.removeEventListener("pause", handlePause);
      video.removeEventListener("timeupdate", handleTimeUpdate);
      video.removeEventListener("loadeddata", handleLoadedData);
      video.removeEventListener("volumechange", handleVolumeChange);
      video.removeEventListener("enterpictureinpicture", handleEnterPiP);
      video.removeEventListener("leavepictureinpicture", handleLeavePiP);

      document.removeEventListener("fullscreenchange", handleFullscreenChange);
      document.removeEventListener("keydown", handleKeyDown);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Sync classes based on states
  useEffect(() => {
    const container = videoContainerRef.current;
    if (!container) return;
    isPaused
      ? container.classList.add("paused")
      : container.classList.remove("paused");
  }, [isPaused]);

  useEffect(() => {
    const container = videoContainerRef.current;
    if (!container) return;
    captionsActive
      ? container.classList.add("captions")
      : container.classList.remove("captions");
  }, [captionsActive]);

  useEffect(() => {
    const container = videoContainerRef.current;
    if (!container) return;
    isTheater
      ? container.classList.add("theater")
      : container.classList.remove("theater");
  }, [isTheater]);

  useEffect(() => {
    const container = videoContainerRef.current;
    if (!container) return;
    isFullscreen
      ? container.classList.add("full-screen")
      : container.classList.remove("full-screen");
  }, [isFullscreen]);

  useEffect(() => {
    const container = videoContainerRef.current;
    if (!container) return;
    isMiniPlayer
      ? container.classList.add("mini-player")
      : container.classList.remove("mini-player");
  }, [isMiniPlayer]);

  // Volume / mute => update <video>
  useEffect(() => {
    if (!videoRef.current) return;
    videoRef.current.volume = volume;
    videoRef.current.muted = muted || volume === 0;
  }, [volume, muted]);

  // Playback rate => update <video>
  useEffect(() => {
    if (!videoRef.current) return;
    videoRef.current.playbackRate = playbackRate;
  }, [playbackRate]);

  // Timeline & scrubbing
  useEffect(() => {
    const timeline = timelineContainerRef.current;
    if (!timeline) return;

    // Because we handle mouse events on timeline and document
    // we unify them below:

    const handleTimelineMouseMove = (e: MouseEvent | ReactMouseEvent) => {
      if (!timeline || !videoRef.current) return;
      const rect = timeline.getBoundingClientRect();
      // Use clientX instead of x
      const clientX = "clientX" in e ? e.clientX : 0;
      const percent = Math.min(Math.max(0, clientX - rect.x), rect.width) / rect.width;

      // Set --preview-position for hover effect
      timeline.style.setProperty("--preview-position", `${percent}`);

      // Decide which preview image to show
      const duration = videoRef.current.duration || 1;
      const previewImgNumber = Math.max(1, Math.floor((percent * duration) / 10));
      const previewImgSrc = `http://localhost:5284/preview/${previewImgNumber}`;


      // If we're just hovering (not scrubbing), show preview
      if (previewImgRef.current) {
        previewImgRef.current.src = previewImgSrc;
      }

      // If scrubbing, update the main thumbnail too, and set progress
      if (isScrubbing) {
        e.preventDefault?.();
        if (thumbnailImgRef.current) {
          thumbnailImgRef.current.src = previewImgSrc;
        }
        timeline.style.setProperty("--progress-position", `${percent}`);
      }
    };

    const handleMouseDown = (e: MouseEvent | ReactMouseEvent) => {
      if ((e as MouseEvent).button !== 0) return; // Left click only
      toggleScrubbing(e);
    };

    const handleMouseUp = (e: MouseEvent) => {
      if (isScrubbing) {
        toggleScrubbing(e);
      }
    };

    const handleMouseMove = (e: MouseEvent) => {
      if (isScrubbing) {
        handleTimelineMouseMove(e);
      }
    };

    // Add event listeners
    timeline.addEventListener("mousemove", handleTimelineMouseMove as any);
    timeline.addEventListener("mousedown", handleMouseDown as any);
    document.addEventListener("mouseup", handleMouseUp);
    document.addEventListener("mousemove", handleMouseMove);

    // Cleanup
    return () => {
      timeline.removeEventListener("mousemove", handleTimelineMouseMove as any);
      timeline.removeEventListener("mousedown", handleMouseDown as any);
      document.removeEventListener("mouseup", handleMouseUp);
      document.removeEventListener("mousemove", handleMouseMove);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isScrubbing]);

  useEffect(() => {
    const container = videoContainerRef.current;
    if (!container) return;
    if (isScrubbing) container.classList.add("scrubbing");
    else container.classList.remove("scrubbing");
  }, [isScrubbing]);

  // --------------------
  // Functions
  // --------------------
  const togglePlay = () => {
    if (!videoRef.current) return;
    if (videoRef.current.paused) {
      videoRef.current.play();
    } else {
      videoRef.current.pause();
    }
  };

  const skip = (seconds: number) => {
    if (!videoRef.current) return;
    videoRef.current.currentTime += seconds;
  };

  const toggleMute = () => {
    setMuted((prev) => !prev);
  };

  const toggleCaptions = () => {
    if (!videoRef.current) return;
    const track = videoRef.current.textTracks[0];
    if (!track) return;

    const isHidden = track.mode === "hidden";
    track.mode = isHidden ? "showing" : "hidden";
    setCaptionsActive(isHidden);
  };

  const changePlaybackSpeed = () => {
    let newRate = playbackRate + 0.25;
    if (newRate > 2) {
      newRate = 0.25;
    }
    setPlaybackRate(newRate);
  };

  const toggleTheaterMode = () => {
    setIsTheater((prev) => !prev);
  };

  const toggleFullScreenMode = () => {
    if (!videoContainerRef.current) return;
    if (!document.fullscreenElement) {
      videoContainerRef.current.requestFullscreen().catch(() => {
        // handle error if user denies or if not available
      });
    } else {
      document.exitFullscreen();
    }
  };

  const toggleMiniPlayerMode = () => {
    if (!videoRef.current) return;
    if (isMiniPlayer) {
      // exit PiP
      document.exitPictureInPicture().catch(() => {
        // handle errors
      });
    } else {
      // enter PiP
      videoRef.current.requestPictureInPicture().catch(() => {
        // handle errors
      });
    }
  };

  // Called on timeline mousedown/mouseup
  const toggleScrubbing = (e: MouseEvent | ReactMouseEvent) => {
    if (!timelineContainerRef.current || !videoRef.current) return;

    const rect = timelineContainerRef.current.getBoundingClientRect();
    const clientX = "clientX" in e ? e.clientX : 0;
    const percent = Math.min(Math.max(0, clientX - rect.x), rect.width) / rect.width;

    // Begin scrubbing
    if ((e as MouseEvent).type === "mousedown" && (e as MouseEvent).button === 0) {
      setIsScrubbing(true);
      setWasPaused(videoRef.current.paused);
      videoRef.current.pause();
    } else {
      // End scrubbing
      setIsScrubbing(false);
      videoRef.current.currentTime = percent * videoRef.current.duration;
      if (!wasPaused) {
        videoRef.current.play();
      }
    }
  };

  // --------------------
  // Render
  // --------------------
  return (
    <div
      className="video-container paused"
      data-volume-level="high"
      ref={videoContainerRef}
    >
      {/* Thumbnail for scrubbing */}
      <img className="thumbnail-img" ref={thumbnailImgRef} alt="" />

      {/* Controls Container */}
      <div className="video-controls-container">
        <div className="timeline-container" ref={timelineContainerRef}>
          <div className="timeline">
            {/* Hover Preview */}
            <img className="preview-img" ref={previewImgRef} alt="" />
            <div className="thumb-indicator"></div>
          </div>
        </div>

        <div className="controls">
          {/* Play/Pause */}
          <button className="play-pause-btn" onClick={togglePlay}>
            <svg className="play-icon" viewBox="0 0 24 24">
              <path fill="currentColor" d="M8,5.14V19.14L19,12.14L8,5.14Z" />
            </svg>
            <svg className="pause-icon" viewBox="0 0 24 24">
              <path fill="currentColor" d="M14,19H18V5H14M6,19H10V5H6V19Z" />
            </svg>
          </button>

          {/* Volume */}
          <div className="volume-container">
            <button className="mute-btn" onClick={toggleMute}>
              <svg className="volume-high-icon" viewBox="0 0 24 24">
                <path
                  fill="currentColor"
                  d="M14,3.23V5.29C16.89,6.15 19,8.83 19,12C19,15.17 
                  16.89,17.84 14,18.7V20.77C18,19.86 21,16.28 21,12C21,7.72 
                  18,4.14 14,3.23M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16C15.5,
                  15.29 16.5,13.76 16.5,12M3,9V15H7L12,20V4L7,9H3Z"
                />
              </svg>
              <svg className="volume-low-icon" viewBox="0 0 24 24">
                <path
                  fill="currentColor"
                  d="M5,9V15H9L14,20V4L9,9M18.5,12C18.5,10.23 
                  17.5,8.71 16,7.97V16C17.5,15.29 18.5,13.76 18.5,12Z"
                />
              </svg>
              <svg className="volume-muted-icon" viewBox="0 0 24 24">
                <path
                  fill="currentColor"
                  d="M12,4L9.91,6.09L12,8.18M4.27,3L3,4.27L7.73,9H3V15H7L12,
                  20V13.27L16.25,17.53C15.58,18.04 14.83,18.46 14,18.7V20.77C15.38,
                  20.45 16.63,19.82 17.68,18.96L19.73,21L21,19.73L12,
                  10.73M19,12C19,12.94 18.8,13.82 18.46,14.64L19.97,
                  16.15C20.62,14.91 21,13.5 21,12C21,7.72 18,
                  4.14 14,3.23V5.29C16.89,6.15 19,8.83 19,
                  12M16.5,12C16.5,10.23 15.5,8.71 14,7.97V10.18L16.45,
                  12.63C16.5,12.43 16.5,12.21 16.5,12Z"
                />
              </svg>
            </button>

            <input
              className="volume-slider"
              type="range"
              min="0"
              max="1"
              step="any"
              value={muted ? 0 : volume}
              onChange={(e) => {
                const val = parseFloat(e.target.value);
                setVolume(val);
                if (val > 0) setMuted(false);
              }}
            />
          </div>

          {/* Duration display (not fully implemented for brevity) */}
          <div className="duration-container">
            <div className="current-time">0:00</div>
            /
            <div className="total-time">--:--</div>
          </div>

          {/* Captions */}
          <button className="captions-btn" onClick={toggleCaptions}>
            <svg viewBox="0 0 24 24">
              <path
                fill="currentColor"
                d="M18,11H16.5V10.5H14.5V13.5H16.5V13H18V14A1,1 
                0 0,1 17,15H14A1,1 0 0,1 13,14V10A1,1 0 0,1 14,
                9H17A1,1 0 0,1 18,10M11,11H9.5V10.5H7.5V13.5H9.5V13H11V14A1,
                1 0 0,1 10,15H7A1,1 0 0,1 6,
                14V10A1,1 0 0,1 7,9H10A1,1 0 0,1 11,10M19,
                4H5C3.89,4 3,4.89 3,6V18A2,2 0 0,0 5,20H19A2,2 0 0,
                0 21,18V6C21,4.89 20.1,4 19,4Z"
              />
            </svg>
          </button>

          {/* Playback Speed */}
          <button className="speed-btn wide-btn" onClick={changePlaybackSpeed}>
            {playbackRate.toFixed(2)}x
          </button>

          {/* Mini-player */}
          <button className="mini-player-btn" onClick={toggleMiniPlayerMode}>
            <svg viewBox="0 0 24 24">
              <path
                fill="currentColor"
                d="M21 3H3c-1.1 0-2 .9-2 
                2v14c0 1.1.9 2 2 2h18c1.1 0 2-.9 
                2-2V5c0-1.1-.9-2-2-2zm0 16H3V5h18v14zm-10-7h9v6h-9z"
              />
            </svg>
          </button>

          {/* Theater Mode */}
          <button className="theater-btn" onClick={toggleTheaterMode}>
            <svg className="tall" viewBox="0 0 24 24">
              <path
                fill="currentColor"
                d="M19 6H5c-1.1 0-2 .9-2 
                2v8c0 1.1.9 2 2 
                2h14c1.1 0 2-.9 
                2-2V8c0-1.1-.9-2-2-2zm0 
                10H5V8h14v8z"
              />
            </svg>
            <svg className="wide" viewBox="0 0 24 24">
              <path
                fill="currentColor"
                d="M19 7H5c-1.1 0-2 
                .9-2 2v6c0 1.1.9 2 2 
                2h14c1.1 0 2-.9 
                2-2V9c0-1.1-.9-2-2-2zm0 
                8H5V9h14v6z"
              />
            </svg>
          </button>

          {/* Full Screen */}
          <button className="full-screen-btn" onClick={toggleFullScreenMode}>
            <svg className="open" viewBox="0 0 24 24">
              <path
                fill="currentColor"
                d="M7 14H5v5h5v-2H7v-3zm-2-4h2V7h3V5H5v5zm12 
                7h-3v2h5v-5h-2v3zM14 5v2h3v3h2V5h-5z"
              />
            </svg>
            <svg className="close" viewBox="0 0 24 24">
              <path
                fill="currentColor"
                d="M5 16h3v3h2v-5H5v2zm3-8H5v2h5V5H8v3zm6 
                11h2v-3h3v-2h-5v5zm2-11V5h-2v5h5V8h-3z"
              />
            </svg>
          </button>
        </div>
      </div>

      {/* VIDEO ELEMENT */}
      <video ref={videoRef} src="http://localhost:5284/video">
        {/* Optional track for captions, if your backend serves it */}
        <track
          kind="captions"
          srcLang="en"
          src="http://localhost:5284/assets/subtitles.vtt"
          default
        />
      </video>
    </div>
  );
};

export default VideoPlayer;
