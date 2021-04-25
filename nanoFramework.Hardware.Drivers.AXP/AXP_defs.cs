using System;
using System.Device.I2c;

namespace nanoFramework.Hardware.Drivers
{
    public abstract partial class AXP
    {
        protected const byte FALLING = 0x02;
        protected const byte RISING = 0x01;

        //Error codes
        protected const int AXP_PASS = 0;
        protected const int AXP_FAIL = -1;
        protected const int AXP_INVALID = -2;
        protected const int AXP_NOT_INIT = -3;
        protected const int AXP_NOT_SUPPORT = -4;
        protected const int AXP_ARG_INVALID = -5;

        //Chip Address
        public const byte AXP202_AGENT_ADDRESS = 0x35;
        public const byte AXP192_AGENT_ADDRESS = 0x34;
        public const byte AXP173_AGENT_ADDRESS = 0x34;

        //Chip ID
        public const byte AXP202_CHIP_ID = 0x41;
        public const byte AXP192_CHIP_ID = 0x03;
        public const byte AXP173_CHIP_ID = 0xAD;   //!Axp173 does not have a chip ID, given a custom ID

        protected const int AXP202_EXTEN = 0;
        protected const int AXP202_DCDC3 = 1;
        protected const int AXP202_LDO2 = 2;
        protected const int AXP202_LDO4 = 3;
        protected const int AXP202_DCDC2 = 4;
        protected const int AXP202_LDO3 = 6;
        protected const int AXP202_OUTPUT_MAX = 7;

        protected const int AXP192_DCDC1 = 0;
        protected const int AXP192_DCDC3 = 1;
        protected const int AXP192_LDO2 = 2;
        protected const int AXP192_LDO3 = 3;
        protected const int AXP192_DCDC2 = 4;
        protected const int AXP192_EXTEN = 6;
        protected const int AXP192_OUTPUT_MAX = 7;

        protected const int AXP173_DCDC1 = 0;
        protected const int AXP173_LDO4 = 1;
        protected const int AXP173_LDO2 = 2;
        protected const int AXP173_LDO3 = 3;
        protected const int AXP173_DCDC2 = 4;
        protected const int AXP173_EXTEN = 6;
        protected const int AXP173_OUTPUT_MAX = 7;

        //RegisterMap
        protected const byte AXP202_STATUS = 0x00;
        protected const byte AXP202_MODE_CHGSTATUS = 0x01;
        protected const byte AXP202_OTG_STATUS = 0x02;
        protected const byte AXP202_IC_TYPE = 0x03;
        protected const byte AXP202_DATA_BUFFER1 = 0x04;
        protected const byte AXP202_DATA_BUFFER2 = 0x05;
        protected const byte AXP202_DATA_BUFFER3 = 0x06;
        protected const byte AXP202_DATA_BUFFER4 = 0x07;
        protected const byte AXP202_DATA_BUFFER5 = 0x08;
        protected const byte AXP202_DATA_BUFFER6 = 0x09;
        protected const byte AXP202_DATA_BUFFER7 = 0x0A;
        protected const byte AXP202_DATA_BUFFER8 = 0x0B;
        protected const byte AXP202_DATA_BUFFER9 = 0x0C;
        protected const byte AXP202_DATA_BUFFERA = 0x0D;
        protected const byte AXP202_DATA_BUFFERB = 0x0E;
        protected const byte AXP202_DATA_BUFFERC = 0x0F;
        protected const byte AXP202_LDO234_DC23_CTL = 0x12;
        protected const byte AXP202_DC2OUT_VOL = 0x23;
        protected const byte AXP202_LDO3_DC2_DVM = 0x25;
        protected const byte AXP202_DC3OUT_VOL = 0x27;
        protected const byte AXP202_LDO24OUT_VOL = 0x28;
        protected const byte AXP202_LDO3OUT_VOL = 0x29;
        protected const byte AXP202_IPS_SET = 0x30;
        protected const byte AXP202_VOFF_SET = 0x31;
        protected const byte AXP202_OFF_CTL = 0x32;
        protected const byte AXP202_CHARGE1 = 0x33;
        protected const byte AXP202_CHARGE2 = 0x34;
        protected const byte AXP202_BACKUP_CHG = 0x35;
        protected const byte AXP202_POK_SET = 0x36;
        protected const byte AXP202_DCDC_FREQSET = 0x37;
        protected const byte AXP202_VLTF_CHGSET = 0x38;
        protected const byte AXP202_VHTF_CHGSET = 0x39;
        protected const byte AXP202_APS_WARNING1 = 0x3A;
        protected const byte AXP202_APS_WARNING2 = 0x3B;
        protected const byte AXP202_TLTF_DISCHGSET = 0x3C;
        protected const byte AXP202_THTF_DISCHGSET = 0x3D;
        protected const byte AXP202_DCDC_MODESET = 0x80;
        protected const byte AXP202_ADC_EN1 = 0x82;
        protected const byte AXP202_ADC_EN2 = 0x83;
        protected const byte AXP202_ADC_SPEED = 0x84;
        protected const byte AXP202_ADC_INPUTRANGE = 0x85;
        protected const byte AXP202_ADC_IRQ_RETFSET = 0x86;
        protected const byte AXP202_ADC_IRQ_FETFSET = 0x87;
        protected const byte AXP202_TIMER_CTL = 0x8A;
        protected const byte AXP202_VBUS_DET_SRP = 0x8B;
        protected const byte AXP202_HOTOVER_CTL = 0x8F;
        protected const byte AXP202_GPIO0_CTL = 0x90;
        protected const byte AXP202_GPIO0_VOL = 0x91;
        protected const byte AXP202_GPIO1_CTL = 0x92;
        protected const byte AXP202_GPIO2_CTL = 0x93;
        protected const byte AXP202_GPIO012_SIGNAL = 0x94;
        protected const byte AXP202_GPIO3_CTL = 0x95;
        protected const byte AXP202_INTEN1 = 0x40;
        protected const byte AXP202_INTEN2 = 0x41;
        protected const byte AXP202_INTEN3 = 0x42;
        protected const byte AXP202_INTEN4 = 0x43;
        protected const byte AXP202_INTEN5 = 0x44;
        protected const byte AXP202_INTSTS1 = 0x48;
        protected const byte AXP202_INTSTS2 = 0x49;
        protected const byte AXP202_INTSTS3 = 0x4A;
        protected const byte AXP202_INTSTS4 = 0x4B;
        protected const byte AXP202_INTSTS5 = 0x4C;

        //Irq control register
        protected const byte AXP192_INTEN1 = 0x40;
        protected const byte AXP192_INTEN2 = 0x41;
        protected const byte AXP192_INTEN3 = 0x42;
        protected const byte AXP192_INTEN4 = 0x43;
        protected const byte AXP192_INTEN5 = 0x4A;

        //Irq status register
        protected const byte AXP192_INTSTS1 = 0x44;
        protected const byte AXP192_INTSTS2 = 0x45;
        protected const byte AXP192_INTSTS3 = 0x46;
        protected const byte AXP192_INTSTS4 = 0x47;
        protected const byte AXP192_INTSTS5 = 0x4D;

        protected const byte AXP192_DC1_VLOTAGE = 0x26;
        protected const byte AXP192_LDO23OUT_VOL = 0x28;
        protected const byte AXP192_GPIO0_CTL = 0x90;
        protected const byte AXP192_GPIO0_VOL = 0x91;
        protected const byte AXP192_GPIO1_CTL = 0X92;
        protected const byte AXP192_GPIO2_CTL = 0x93;
        protected const byte AXP192_GPIO012_SIGNAL = 0x94;
        protected const byte AXP192_GPIO34_CTL = 0x95;

        //AXP 192/202 adc data register
        protected const byte AXP202_BAT_AVERVOL_H8 = 0x78;
        protected const byte AXP202_BAT_AVERVOL_L4 = 0x79;
        protected const byte AXP202_BAT_AVERCHGCUR_H8 = 0x7A;
        protected const byte AXP202_BAT_AVERCHGCUR_L4 = 0x7B;
        protected const byte AXP202_BAT_AVERCHGCUR_L5 = 0x7B;
        protected const byte AXP202_ACIN_VOL_H8 = 0x56;
        protected const byte AXP202_ACIN_VOL_L4 = 0x57;
        protected const byte AXP202_ACIN_CUR_H8 = 0x58;
        protected const byte AXP202_ACIN_CUR_L4 = 0x59;
        protected const byte AXP202_VBUS_VOL_H8 = 0x5A;
        protected const byte AXP202_VBUS_VOL_L4 = 0x5B;
        protected const byte AXP202_VBUS_CUR_H8 = 0x5C;
        protected const byte AXP202_VBUS_CUR_L4 = 0x5D;
        protected const byte AXP202_INTERNAL_TEMP_H8 = 0x5E;
        protected const byte AXP202_INTERNAL_TEMP_L4 = 0x5F;
        protected const byte AXP202_TS_IN_H8 = 0x62;
        protected const byte AXP202_TS_IN_L4 = 0x63;
        protected const byte AXP202_GPIO0_VOL_ADC_H8 = 0x64;
        protected const byte AXP202_GPIO0_VOL_ADC_L4 = 0x65;
        protected const byte AXP202_GPIO1_VOL_ADC_H8 = 0x66;
        protected const byte AXP202_GPIO1_VOL_ADC_L4 = 0x67;

        protected const byte AXP202_BAT_AVERDISCHGCUR_H8 = 0x7C;
        protected const byte AXP202_BAT_AVERDISCHGCUR_L5 = 0x7D;
        protected const byte AXP202_APS_AVERVOL_H8 = 0x7E;
        protected const byte AXP202_APS_AVERVOL_L4 = 0x7F;
        protected const byte AXP202_INT_BAT_CHGCUR_H8 = 0xA0;
        protected const byte AXP202_INT_BAT_CHGCUR_L4 = 0xA1;
        protected const byte AXP202_EXT_BAT_CHGCUR_H8 = 0xA2;
        protected const byte AXP202_EXT_BAT_CHGCUR_L4 = 0xA3;
        protected const byte AXP202_INT_BAT_DISCHGCUR_H8 = 0xA4;
        protected const byte AXP202_INT_BAT_DISCHGCUR_L4 = 0xA5;
        protected const byte AXP202_EXT_BAT_DISCHGCUR_H8 = 0xA6;
        protected const byte AXP202_EXT_BAT_DISCHGCUR_L4 = 0xA7;
        protected const byte AXP202_BAT_CHGCOULOMB3 = 0xB0;
        protected const byte AXP202_BAT_CHGCOULOMB2 = 0xB1;
        protected const byte AXP202_BAT_CHGCOULOMB1 = 0xB2;
        protected const byte AXP202_BAT_CHGCOULOMB0 = 0xB3;
        protected const byte AXP202_BAT_DISCHGCOULOMB3 = 0xB4;
        protected const byte AXP202_BAT_DISCHGCOULOMB2 = 0xB5;
        protected const byte AXP202_BAT_DISCHGCOULOMB1 = 0xB6;
        protected const byte AXP202_BAT_DISCHGCOULOMB0 = 0xB7;
        protected const byte AXP202_COULOMB_CTL = 0xB8;
        protected const byte AXP202_BAT_POWERH8 = 0x70;
        protected const byte AXP202_BAT_POWERM8 = 0x71;
        protected const byte AXP202_BAT_POWERL8 = 0x72;

        protected const byte AXP202_VREF_TEM_CTRL = 0xF3;
        protected const byte AXP202_BATT_PERCENTAGE = 0xB9;

        //bit definitions for AXP events, irq event
        //AXP202
        protected const byte AXP202_IRQ_USBLO = 1;
        protected const byte AXP202_IRQ_USBRE = 2;
        protected const byte AXP202_IRQ_USBIN = 3;
        protected const byte AXP202_IRQ_USBOV = 4;
        protected const byte AXP202_IRQ_ACRE = 5;
        protected const byte AXP202_IRQ_ACIN = 6;
        protected const byte AXP202_IRQ_ACOV = 7;

        protected const byte AXP202_IRQ_TEMLO = 8;
        protected const byte AXP202_IRQ_TEMOV = 9;
        protected const byte AXP202_IRQ_CHAOV = 10;
        protected const byte AXP202_IRQ_CHAST = 11;
        protected const byte AXP202_IRQ_BATATOU = 12;
        protected const byte AXP202_IRQ_BATATIN = 13;
        protected const byte AXP202_IRQ_BATRE = 14;
        protected const byte AXP202_IRQ_BATIN = 15;

        protected const byte AXP202_IRQ_POKLO = 16;
        protected const byte AXP202_IRQ_POKSH = 17;
        protected const byte AXP202_IRQ_LDO3LO = 18;
        protected const byte AXP202_IRQ_DCDC3LO = 19;
        protected const byte AXP202_IRQ_DCDC2LO = 20;
        protected const byte AXP202_IRQ_CHACURLO = 22;
        protected const byte AXP202_IRQ_ICTEMOV = 23;

        protected const byte AXP202_IRQ_EXTLOWARN2 = 24;
        protected const byte AXP202_IRQ_EXTLOWARN1 = 25;
        protected const byte AXP202_IRQ_SESSION_END = 26;
        protected const byte AXP202_IRQ_SESS_AB_VALID = 27;
        protected const byte AXP202_IRQ_VBUS_UN_VALID = 28;
        protected const byte AXP202_IRQ_VBUS_VALID = 29;
        protected const byte AXP202_IRQ_PDOWN_BY_NOE = 30;
        protected const byte AXP202_IRQ_PUP_BY_NOE = 31;

        protected const byte AXP202_IRQ_GPIO0TG = 32;
        protected const byte AXP202_IRQ_GPIO1TG = 33;
        protected const byte AXP202_IRQ_GPIO2TG = 34;
        protected const byte AXP202_IRQ_GPIO3TG = 35;
        protected const byte AXP202_IRQ_PEKFE = 37;
        protected const byte AXP202_IRQ_PEKRE = 38;
        protected const byte AXP202_IRQ_TIMER = 39;

        //Signal Capture
        protected const float AXP202_BATT_VOLTAGE_STEP = 1.1F;
        protected const float AXP202_BATT_DISCHARGE_CUR_STEP = 0.5F;
        protected const float AXP202_BATT_CHARGE_CUR_STEP = 0.5F;
        protected const float AXP202_ACIN_VOLTAGE_STEP = 1.7F;
        protected const float AXP202_ACIN_CUR_STEP = 0.625F;
        protected const float AXP202_VBUS_VOLTAGE_STEP = 1.7F;
        protected const float AXP202_VBUS_CUR_STEP = 0.375F;
        protected const float AXP202_INTERNAL_TEMP_STEP = 0.1F;
        protected const float AXP202_INTERNAL_TEMP_MIN = -144.7F;
        protected const float AXP202_APS_VOLTAGE_STEP = 1.4F;
        protected const float AXP202_TS_PIN_OUT_STEP = 0.8F;
        protected const float AXP202_GPIO0_STEP = 0.5F;
        protected const float AXP202_GPIO1_STEP = 0.5F;
        // AXP192 only
        protected const float AXP202_GPIO2_STEP = 0.5F;
        protected const float AXP202_GPIO3_STEP = 0.5F;

        // AXP173
        protected const byte AXP173_EXTEN_DC2_CTL   = 0x10;
        protected const byte AXP173_CTL_DC2_BIT      = 0;
        protected const byte AXP173_CTL_EXTEN_BIT    = 2;
        protected const byte AXP173_DC1_VLOTAGE      = 0x26;
        protected const byte AXP173_LDO4_VOLTAGE     = 0x27;

        //#define BIT_MASK(x) (1 << x)
        //#define IS_OPEN(reg, channel) (bool)(reg & BIT_MASK(channel))

        protected static bool IsOpen(byte reg, byte channel) => (reg & BitMask(channel)) > 0;
        protected bool IsOpenOREG(byte channel) => IsOpen(OutputRegister, channel);
    }

    public enum AXP202FunctionsADC2 : byte
    {
        TempMonitoring = 1 << 7,
        GPIO1 = 1 << 3,
        GPIO0 = 1 << 2
    }

    public enum AXPVoltageTableLDO5 : byte
    {
        LDO5_1800MV,
        LDO5_2500MV,
        LDO5_2800MV,
        LDO5_3000MV,
        LDO5_3100MV,
        LDO5_3300MV,
        LDO5_3400MV,
        LDO5_3500MV,
    }

    public enum AXP_GPIO : byte
    {
        GPIO0,
        GPIO1,
        GPIO2,
        GPIO3,
        GPIO4
    }

    public enum AXP_GPIOMode : byte
    {
        OutputLow,
        OutputHigh,
        Input,
        LDO,
        ADC,
        Floating,
        OpenDrainOutput,
        PWMOutput,
        ExternChargingCtrl,
    }

    public enum AXP_GPIO_IRQ : byte
    {
        None,
        Rising,
        Falling,
        DoubleEdge
    }

    public enum AXP192GPIOVoltage : byte
    {
        GPIO_1v8,
        GPIO_1v9,
        GPIO_2v0,
        GPIO_2v1,
        GPIO_2v2,
        GPIO_2v3,
        GPIO_2v4,
        GPIO_2v5,
        GPIO_2v6,
        GPIO_2v7,
        GPIO_2v8,
        GPIO_2v9,
        GPIO_3v0,
        GPIO_3v1,
        GPIO_3v2,
        GPIO_3v3,
    }

    public enum AXP1xxChargeCurrent : byte
    {
        Current_100MA,
        Current_190MA,
        Current_280MA,
        Current_360MA,
        Current_450MA,
        Current_550MA,
        Current_630MA,
        Current_700MA,
        Current_780MA,
        Current_880MA,
        Current_960MA,
        Current_1000MA,
        Current_1080MA,
        Current_1160MA,
        Current_1240MA,
        Current_1320MA,
    }

    public enum AXP202LDO3Mode : byte
    {
        LDO,
        DCIN
    }

    public enum AXPTableLDO4 : byte
    {
        LDO4_1250MV,
        LDO4_1300MV,
        LDO4_1400MV,
        LDO4_1500MV,
        LDO4_1600MV,
        LDO4_1700MV,
        LDO4_1800MV,
        LDO4_1900MV,
        LDO4_2000MV,
        LDO4_2500MV,
        LDO4_2700MV,
        LDO4_2800MV,
        LDO4_3000MV,
        LDO4_3100MV,
        LDO4_3200MV,
        LDO4_3300MV,
        LDO4_MAX,
    }

    public enum AXP202StartupTime : byte
    {
        Time3s,
        Time2s,
        Time1s,
        Time128Ms,
    }

    public enum AXP192StartupTime : byte
    {
        Time2s,
        Time1s,
        Time512Ms,
        Time128Ms,
    }

    public enum AXPLongPressTime : byte
    {
        Time1s,
        Time1s5,
        Time2s,
        Time2s5
    }

    public enum AXPPowerOffTime : byte
    {
        Time4s,
        Time6s,
        Time8s,
        Time16s
    }

    public enum AXP202ChargingVoltage : byte
    {
        Voltage_4_1V,
        /// <summary>
        /// RECOMMENDED
        /// </summary>
        Voltage_4_15V,
        Voltage_4_2V,
        Voltage_4_36V,
    }

    public enum AXP202FunctionsADC1 : byte
    {
        BatteryVoltage = 1 << 7,
        BatteryCurrent = 1 << 6,
        ACINVoltage = 1 << 5,
        ACINCurrent = 1 << 4,
        VBUSVoltage = 1 << 3,
        VBUSCurrent = 1 << 2,
        APSVoltage = 1 << 1,
        TSPin = 1 << 0
    }

    public enum AXP_TSPinCurrent : byte
    {
        Current_20uA,
        Current_40uA,
        Current_60uA,
        Current_80uA
    }

    public enum AXP_TSPinFunction : byte
    {
        FunctionBattery,
        FunctionADC
    }

    public enum AXP_TSPinMode : byte
    {
        Disable,
        Charging,
        Sampling,
        Enable
    }

    public enum AXPChargeLEDMode : byte
    {
        Off,
        Blink_1Hz,
        Blink_4Hz,
        LowLevel
    }

    public enum AXPSamplingRateADC : byte
    {
        Rate_25Hz,
        Rate_50Hz,
        Rate_100Hz,
        Rate_200Hz
    }

    public enum AXPIRQ : ulong
    {
        //! IRQ1 REG 40H
        VBUS_VHOLD_LOW = 1ul << 1,   //VBUS is available, but lower than V HOLD, IRQ enable
        VBUS_REMOVED = 1ul << 2,   //VBUS removed, IRQ enable
        VBUS_CONNECT = 1ul << 3,   //VBUS connected, IRQ enable
        VBUS_OVER_VOL = 1ul << 4,   //VBUS over-voltage, IRQ enable
        ACIN_REMOVED = 1ul << 5,   //ACIN removed, IRQ enable
        ACIN_CONNECT = 1ul << 6,   //ACIN connected, IRQ enable
        ACIN_OVER_VOL = 1ul << 7,   //ACIN over-voltage, IRQ enable

        //! IRQ2 REG 41H
        BATT_LOW_TEMP = 1ul << 8,   //Battery low-temperature, IRQ enable
        BATT_OVER_TEMP = 1ul << 9,   //Battery over-temperature, IRQ enable
        CHARGING_FINISHED = 1ul << 10,  //Charge finished, IRQ enable
        CHARGING = 1ul << 11,  //Be charging, IRQ enable
        BATT_EXIT_ACTIVATE = 1ul << 12,  //Exit battery activate mode, IRQ enable
        BATT_ACTIVATE = 1ul << 13,  //Battery activate mode, IRQ enable
        BATT_REMOVED = 1ul << 14,  //Battery removed, IRQ enable
        BATT_CONNECT = 1ul << 15,  //Battery connected, IRQ enable

        //! IRQ3 REG 42H
        PEK_LONGPRESS = 1ul << 16,  //PEK long press, IRQ enable
        PEK_SHORTPRESS = 1ul << 17,  //PEK short press, IRQ enable
        LDO3_LOW_VOL = 1ul << 18,  //LDO3output voltage is lower than the set value, IRQ enable
        DC3_LOW_VOL = 1ul << 19,  //DC-DC3output voltage is lower than the set value, IRQ enable
        DC2_LOW_VOL = 1ul << 20,  //DC-DC2 output voltage is lower than the set value, IRQ enable
                                            //**Reserved and unchangeable BIT 5
        CHARGE_LOW_CUR = 1ul << 22,  //Charge current is lower than the set current, IRQ enable
        CHIP_TEMP_HIGH = 1ul << 23,  //AXP202 internal over-temperature, IRQ enable

        //! IRQ4 REG 43H
        APS_LOW_VOL_LEVEL2 = 1ul << 24,  //APS low-voltage, IRQ enable（LEVEL2）
        APX202_APS_LOW_VOL_LEVEL1 = 1ul << 25,  //APS low-voltage, IRQ enable（LEVEL1）
        VBUS_SESSION_END = 1ul << 26,  //VBUS Session End IRQ enable
        VBUS_SESSION_AB = 1ul << 27,  //VBUS Session A/B IRQ enable
        VBUS_INVALID = 1ul << 28,  //VBUS invalid, IRQ enable
        VBUS_VAILD = 1ul << 29,  //VBUS valid, IRQ enable
        NOE_OFF = 1ul << 30,  //N_OE shutdown, IRQ enable
        NOE_ON = 1ul << 31,  //N_OE startup, IRQ enable

        //! IRQ5 REG 44H
        GPIO0_EDGE_TRIGGER = 1ul << 32,  //GPIO0 input edge trigger, IRQ enable
        GPIO1_EDGE_TRIGGER = 1ul << 33,  //GPIO1input edge trigger or ADC input, IRQ enable
        GPIO2_EDGE_TRIGGER = 1ul << 34,  //GPIO2input edge trigger, IRQ enable
        GPIO3_EDGE_TRIGGER = 1ul << 35,  //GPIO3 input edge trigger, IRQ enable
                                                    //**Reserved and unchangeable BIT 4
        PEK_FALLING_EDGE = 1ul << 37,  //PEK press falling edge, IRQ enable
        PEK_RISING_EDGE = 1ul << 38,  //PEK press rising edge, IRQ enable
        TIMER_TIMEOUT = 1ul << 39,  //Timer timeout, IRQ enable

        ALL = 0xFFFFFFFFFFul
    }
}
