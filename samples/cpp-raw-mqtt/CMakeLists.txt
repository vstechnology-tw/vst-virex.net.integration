cmake_minimum_required(VERSION 3.20)
project(CppRawMqttSample LANGUAGES CXX)

add_executable(cpp-raw-mqtt main.cpp)
target_compile_features(cpp-raw-mqtt PRIVATE cxx_std_17)
target_link_libraries(cpp-raw-mqtt PRIVATE ws2_32)

