#!/bin/bash
# Cross-platform build script for RemoteDesktopServer
# Usage: ./build.sh [debug|release]

# Set default build type
BUILD_TYPE="release"
if [ "$1" == "debug" ]; then
    BUILD_TYPE="debug"
fi

# Create build directory
mkdir -p build
cd build

# Detect platform
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "Building for Linux..."
    
    # Check for required dependencies
    if ! command -v cmake &> /dev/null; then
        echo "Error: cmake not found. Please install cmake."
        exit 1
    fi
    
    if ! command -v g++ &> /dev/null; then
        echo "Error: g++ not found. Please install g++."
        exit 1
    fi
    
    # Check for X11 dependency
    if ! pkg-config --exists x11; then
        echo "Error: X11 development libraries not found. Please install libx11-dev."
        exit 1
    fi
    
    # Configure and build
    cmake .. -DCMAKE_BUILD_TYPE=$BUILD_TYPE -DENABLE_IPC=ON
    make -j$(nproc)
    
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Building for macOS..."
    
    # Check for required dependencies
    if ! command -v cmake &> /dev/null; then
        echo "Error: cmake not found. Please install cmake (brew install cmake)."
        exit 1
    fi
    
    if ! command -v clang++ &> /dev/null; then
        echo "Error: clang++ not found. Please install Xcode command line tools."
        exit 1
    fi
    
    # Configure and build
    cmake .. -DCMAKE_BUILD_TYPE=$BUILD_TYPE -DENABLE_IPC=ON
    make -j$(sysctl -n hw.logicalcpu)
    
elif [[ "$OSTYPE" == "cygwin" ]] || [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "win32" ]] || command -v cl &> /dev/null; then
    echo "Building for Windows..."
    
    # Check for CMake
    if ! command -v cmake &> /dev/null; then
        echo "Error: cmake not found. Please install cmake."
        exit 1
    fi
    
    # Try to detect Visual Studio
    if command -v vswhere &> /dev/null; then
        VS_PATH=$(vswhere -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath)
        if [ -n "$VS_PATH" ]; then
            echo "Visual Studio found at: $VS_PATH"
            # Configure and build with Visual Studio
            cmake .. -G "Visual Studio 16 2019" -A x64 -DENABLE_IPC=ON
            cmake --build . --config $BUILD_TYPE
        else
            echo "Visual Studio not found, using MinGW"
            cmake .. -G "MinGW Makefiles" -DCMAKE_BUILD_TYPE=$BUILD_TYPE -DENABLE_IPC=ON
            mingw32-make -j$(nproc)
        fi
    else
        echo "Using default generator"
        cmake .. -DCMAKE_BUILD_TYPE=$BUILD_TYPE -DENABLE_IPC=ON
        cmake --build . --config $BUILD_TYPE
    fi
else
    echo "Unsupported platform: $OSTYPE"
    exit 1
fi

echo "Build complete: $BUILD_TYPE configuration"

# Create installation package (basic example)
if [ "$BUILD_TYPE" == "release" ]; then
    echo "Creating installation package..."
    mkdir -p ../dist
    cp RemoteDesktopServer ../dist/
    
    # Copy any needed dependencies
    # cp /path/to/dependency ../dist/
    
    echo "Installation package created in ./dist/"
fi

cd ..
echo "Done!"