export default {
    field() {
        return {
            System: {
                RegistryValues: [],
                RegistryKeys: {},
                RegistrysCount: 0,

                Cpu: 0,
                Memory: 0,
                Disk: 0,
                Drives: [],
                getColor(value) {
                    let color = '#0bd10b';
                    if (value >= 0.8) {
                        color = '#fc0202';
                    } else if (value >= 0.5) {
                        color = '#ff9d1c';
                    }
                    return color;
                },
                draw(canvas, ctx) {

                    const space = 10;

                    const arr = [
                        { text: 'cpu', value: this.Cpu / 100 },
                        { text: 'memory', value: this.Memory / 100 },
                        // { text: 'disk', value: parseInt(this.Disk) },
                    ].concat(this.Drives.map(c => {
                        return {
                            text: c.Name.toLowerCase(),
                            value: (1 - c.Free / c.Total).toFixed(2)
                        };
                    }));
                    for (let i = 0; i < arr.length; i++) {
                        const width = canvas.width * 1 / arr.length;
                        const pos = i * width;
                        const item = arr[i];

                        ctx.beginPath();
                        ctx.fillStyle = 'rgba(255,255,255,0.2)';
                        ctx.fillRect(pos, canvas.height - 10, width - space, 10);
                        ctx.fillStyle = this.getColor(item.value);
                        ctx.fillRect(pos, canvas.height - 10, width * (item.value) - space, 10);
                        ctx.closePath();

                        ctx.lineWidth = 5;
                        ctx.font = 'bold 60px Arial';
                        ctx.fillStyle = 'rgba(0,0,0,0.5)';
                        ctx.fillText(`${item.text} ${(item.value * 100).toFixed(0)}`, pos, canvas.height - 20);
                        ctx.lineWidth = 2;
                        ctx.strokeStyle = '#fff';
                        ctx.strokeText(`${item.text} ${(item.value * 100).toFixed(0)}`, pos, canvas.height - 20);
                    }
                    //this.Cpu--;
                    //this.Memory--;
                    //this.Disk--;
                    if (this.Cpu < 0) {
                        this.Cpu = 0;
                    }
                    if (this.Memory < 0) {
                        this.Memory = 0;
                    }
                    if (this.Disk < 0) {
                        this.Disk = 0;
                    }
                }
            }
        };
    },
    state: {
        system: {
            showSystemOptions: false,
            devices: []
        }
    },
    init() {
    },

    update(item, report) {
        if (!report.System) return;

        item.System.Cpu = report.System.Cpu;
        item.System.Memory = report.System.Memory;
        item.System.Disk = report.System.Disk;
        if (report.System.Drives) {
            item.lastUpdated = Date.now();
            item.System.Drives = report.System.Drives || [];
        }

        if (report.System.RegValues) {
            item.System.RegistryValues = report.System.RegValues.split('').map(c => +c);
            item.System.RegistrysCount = item.System.RegistryValues.filter(c => c == '1').length;
        }
        if (report.System.RegKeys) {
            item.System.RegistryKeys = report.System.RegKeys;
        }

    }
}