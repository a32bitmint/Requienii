﻿using ProjectDMG.Utils;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace ProjectDMG {
    public class ProjectDMG {

        private PictureBox pictureBox;
        private CPU cpu;
        private MMU mmu;
        private PPU ppu;
        private TIMER timer;
        public JOYPAD joypad;
        private Thread cpuThread;

        public bool power_switch = false;

        public ProjectDMG(PictureBox pictureBox) {

            this.pictureBox = pictureBox;

        }

        public void POWER_ON() {
            cpu = new CPU();
            mmu = new MMU();
            ppu = new PPU(pictureBox);
            timer = new TIMER();
            joypad = new JOYPAD();

            mmu.loadGamePak();
            mmu.loadBootRom();

            power_switch = true;
            cpuThread = new Thread(new ThreadStart(EXECUTE));
            cpuThread.IsBackground = true;
            cpuThread.Start();
        }

        public void POWER_OFF() {
            power_switch = false;
            if (cpuThread != null && cpuThread.IsAlive) cpuThread.Join();
        }

        public void EXECUTE() { // Main Loop Work in progress
            long start = nanoTime();
            long elapsed = nanoTime();
            int cpuCycles = 0;
            int cyclesThisUpdate = 0;
            //int dev = 0;

            while (power_switch) {

                if ((elapsed - start) >= 16740000) { //nanoseconds per frame
                    start += 16740000;
                    while (cyclesThisUpdate < Constants.CYCLES_PER_UPDATE) {
                        cpuCycles = cpu.Exe(mmu);
                        cyclesThisUpdate += cpuCycles;

                        timer.update(cpuCycles, mmu);
                        ppu.update(cpuCycles, mmu);
                        joypad.update(mmu);
                        handleInterrupts(mmu, cpu);
                    }
                    cyclesThisUpdate -= Constants.CYCLES_PER_UPDATE;
                    //dev = 0;
                }
                elapsed = nanoTime();
                //Console.WriteLine(dev++ +" " +  (elapsed-start)/1000);
                //if ((elapsed - start) < 12000000) Thread.Sleep(1);
                //Busy waiting :( but sleeping the thread equals to choppy frame rate
            }
        }

        private void handleInterrupts(MMU mmu, CPU cpu) {
            if (mmu.IF != 0) {
                for (byte i = 0; i < 5; i++) {
                    if (mmu.isBit(i, mmu.IE) && mmu.isBit(i, mmu.IF)) {
                        cpu.ExecuteInterrupt(mmu, i);
                    }
                }
            }
        }

        private static long nanoTime() {
            long nano = 10000L * Stopwatch.GetTimestamp();
            nano /= TimeSpan.TicksPerMillisecond;
            nano *= 100L;
            return nano;
        }

    }


}
