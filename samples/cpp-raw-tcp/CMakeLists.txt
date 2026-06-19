cmake_minimum_required(VERSION 3.20)
project(CppRawTcpSample LANGUAGES CXX)

add_executable(cpp-raw-tcp main.cpp)
target_compile_features(cpp-raw-tcp PRIVATE cxx_std_17)
target_link_libraries(cpp-raw-tcp PRIVATE ws2_32)

