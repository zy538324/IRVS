cmake_minimum_required(VERSION 3.10)
project(RemoteDesktopServer VERSION 1.0.0)

# C++ standard
set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

# Build type
if(NOT CMAKE_BUILD_TYPE)
    set(CMAKE_BUILD_TYPE Release)
endif()

# Compiler flags
if(MSVC)
    add_compile_options(/W4 /WX- /MP /EHsc)
    add_definitions(-D_CRT_SECURE_NO_WARNINGS)
else()
    add_compile_options(-Wall -Wextra -pedantic)
endif()

# Enable IPC option
option(ENABLE_IPC "Enable Inter-Process Communication" OFF)
if(ENABLE_IPC)
    add_definitions(-DENABLE_IPC)
    include_directories(${CMAKE_SOURCE_DIR}/../../AgentCore)
endif()

# Check platform and add appropriate dependencies
if(WIN32)
    # Windows specific dependencies
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    set(PLATFORM_LIBS ws2_32 gdi32 gdiplus)
elseif(APPLE)
    # macOS specific dependencies
    find_library(CARBON_LIBRARY Carbon)
    find_library(COCOA_LIBRARY Cocoa)
    set(PLATFORM_LIBS ${CARBON_LIBRARY} ${COCOA_LIBRARY})
else()
    # Linux specific dependencies
    find_package(X11 REQUIRED)
    include_directories(${X11_INCLUDE_DIR})
    set(PLATFORM_LIBS ${X11_LIBRARIES})
    # Check for other Linux dependencies
    find_package(Threads REQUIRED)
    list(APPEND PLATFORM_LIBS ${CMAKE_THREAD_LIBS_INIT})
endif()

# Source files
set(SOURCES
    main.cpp
    RemoteDesktopServer.cpp
)

# Header files
set(HEADERS
    RemoteDesktopServer.h
)

# Add executable
add_executable(RemoteDesktopServer ${SOURCES} ${HEADERS})

# Link libraries
target_link_libraries(RemoteDesktopServer ${PLATFORM_LIBS})

# Install
install(TARGETS RemoteDesktopServer DESTINATION bin)

# Generate version header
configure_file(
    ${CMAKE_SOURCE_DIR}/version.h.in
    ${CMAKE_BINARY_DIR}/generated/version.h
)
include_directories(${CMAKE_BINARY_DIR}/generated)

# Output directory
set_target_properties(RemoteDesktopServer PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_BINARY_DIR}"
)

# Print configuration
message(STATUS "Build type: ${CMAKE_BUILD_TYPE}")
message(STATUS "IPC enabled: ${ENABLE_IPC}")
message(STATUS "Platform libraries: ${PLATFORM_LIBS}")