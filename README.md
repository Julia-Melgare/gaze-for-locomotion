# Simulating Gaze and Visual Attention of Walking Characters in Dynamic Environments

### Requirements

- Unity 2021.3.8f1
- Python 3.6.8 &
- CUDA 10
- cuDNN 7.4

### Instructions

Clone the main project from [GitHub](https://github.com/Virtual-Humans-Lab/VisionCrowds)

```bash
git clone https://github.com/Virtual-Humans-Lab/VisionCrowds
```

Install CUDA 10 from [here](https://developer.nvidia.com/cuda-10.0-download-archive?target_os=Windows&target_arch=x86_64&target_version=10&target_type=exenetwork) - Make sure to choose “Custom Installation” and deselect any other options except the actual CUDA drivers.

Download cuDNN 7.4 for CUDA 10 from [here](https://developer.nvidia.com/rdp/cudnn-archive)

After downloading, extract it to path `C:\tools\cuda` 

Add all necessary references from CUDA and cuDNN to PATH (as explained [here](https://www.tensorflow.org/install/gpu?hl=pt-br#windows_setup) - it’s in portuguese because the original english page does not exist anymore)

```powershell
SET PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v10.0\bin;%PATH%
SET PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v10.0\extras\CUPTI\lib64;%PATH%
SET PATH=C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v10.0\include;%PATH%
SET PATH=C:\tools\cuda\bin;%PATH%
```

Install Python dependencies:

```powershell
pip install tensorflow-gpu==1.13.1
pip install matplotlib==3.0.3
pip install requests==2.21.0
pip install gdown==4.6.3
pip install scipy==1.4.1
pip install zmq
```

OR

```
pip install -r Assets/PythonServer_ImageSal/requirements.txt
```

Install OpenCV for Python as explained [here](https://docs.opencv.org/4.x/d5/de5/tutorial_py_setup_in_windows.html) in the session “**Installing OpenCV from prebuilt binaries”.**

Prior to running the project in Unity, start the Python server in a separate terminal:

```powershell
python Assets/PythonServer/main.py
```

