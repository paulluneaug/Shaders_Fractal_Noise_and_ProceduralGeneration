def NextPowerOfTwo(n : int):
    shift = 0
    a = False
    isPowerOfTwo = n > 0
    while (n != 0):
        if (a):
            isPowerOfTwo = False
            a = False
        if (n & 1 == 1):
            a = True
        
        shift += 1
        n = n >> 1
    return shift - isPowerOfTwo

def NextMultipleOf(n : int, mul : int):
    return n + (n % mul != 0) * mul
    
def LocalOffsetToLocalCoordinates2D(offset : int) -> tuple:
    x = 0
    y = 0
    rst = offset
    
    pow = NextMultipleOf(NextPowerOfTwo(offset), 2) // 2
    while (pow >= 0):
        localOffset = OffsetToLocalOffsetInSqareOfDim(rst, pow)
        coords = OffsetToCoordinatesInUnitSquare(localOffset[0])
        x += coords[0] * (1 << pow)
        y += coords[1] * (1 << pow)
        rst = localOffset[1]
        pow -= 1
    
    return (x, y)

def OffsetToLocalOffsetInSqareOfDim(offset, pow):
    dim = (1 << ((2 * pow)))
    return (offset // dim, offset % dim)

def OffsetToCoordinatesInUnitSquare(offset):
    return (offset % 2, offset // 2)

    
def LocalOffsetToLocalCoordinates3D(offset : int) -> tuple:
    x = 0
    y = 0
    z = 0
    rst = offset
    
    pow = NextMultipleOf(NextPowerOfTwo(offset), 3) // 3
    while (pow >= 0):
        localOffset = OffsetToLocalOffsetInCubeOfDim(rst, pow)
        coords = OffsetToCoordinatesInUnitCube(localOffset[0])
        x += coords[0] * (1 << pow)
        y += coords[1] * (1 << pow)
        z += coords[2] * (1 << pow)
        rst = localOffset[1]
        pow -= 1
    
    return (x, y, z)

def OffsetToLocalOffsetInCubeOfDim(offset, pow):
    dim = (1 << ((3 * pow)))
    return (offset // dim, offset % dim)

def OffsetToCoordinatesInUnitCube(offset):
    z = offset // 4
    offset = offset % 4
    return (offset % 2, offset // 2, z)

size = 4
l = [[[0 for _ in range(size)] for _ in range(size)] for _ in range(size)]

for i in range(size * size * size):
    coords = LocalOffsetToLocalCoordinates3D(i)
    print(coords, i)
    print("\n")
    l[coords[2]][coords[1]][coords[0]] = i
    
for iz in range(size):
    for iy in range(size):
        print(l[iz][iy])

print(OffsetToCoordinatesInUnitSquare(11, 2))


#  0  1  4  5 
#  2  3  6  7
#  8  9 12 13
# 10 11 14 15