// Dashboard: tarih filtresi, KPI kartları ve grafikler. Veri /Dashboard/Data ucundan gelir, toplulaştırma SQL'de yapılır.
(function () {
    const form = document.getElementById('dashFilter');
    const dStart = document.getElementById('dStart');
    const dEnd = document.getElementById('dEnd');
    const dReset = document.getElementById('dReset');
    const status = document.getElementById('dashStatus');
    const productSort = document.getElementById('productSort');

    const numFmt = new Intl.NumberFormat('tr-TR');
    const moneyWhole = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 });
    const moneyCents = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });

    let requestId = 0; // geç gelen eski yanıtların yenisini ezmesini engeller

    let trendChart = null;
    let statusChart = null;
    let productsChart = null;
    const monthNames = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];

    const rankCharts = {}; // şehir/kategori grafik örnekleri (yeniden çizmek için saklanır)

    // Durum renkleri (pastel); sipariş listesindeki rozetlerle uyumlu
    const statusColors = {
        'Tamamlandı': '#7fd1b0',
        'Beklemede': '#f4b183',
        'Kargoda': '#7ea6f0',
        'İptal Edildi': '#b4a7e8',
        'İade Edildi': '#f3a6b8'
    };

    // "2026-11-19T00:00:00" -> "2026-11-19"
    const dateOnly = iso => iso.slice(0, 10);

    // Sunucudan gelen bitiş tarihi "hariç" sınırdır; ekranda "dahil" son gün olarak gösterilir
    function minusOneDay(iso) {
        const [y, m, d] = dateOnly(iso).split('-').map(Number);
        const dt = new Date(y, m - 1, d - 1);
        const pad = n => String(n).padStart(2, '0');
        return `${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(dt.getDate())}`;
    }

    function setText(id, text) {
        document.getElementById(id).textContent = text;
    }

    function renderKpis(k) {
        setText('kpiOrders', numFmt.format(k.toplamSiparis));
        setText('kpiRevenue', moneyWhole.format(k.toplamCiro));
        setText('kpiCustomers', numFmt.format(k.toplamMusteri));
        setText('kpiBasket', moneyCents.format(k.ortalamaSepet));
    }

    // Aylık trend: sütun = satış tutarı (sol eksen), çizgi = sipariş sayısı (sağ eksen)
    function renderTrend(rows, startIso, endExclusiveIso) {
        const labels = rows.map(r => `${monthNames[r.ay - 1]} ${String(r.yil).slice(2)}`);
        const sales = rows.map(r => r.toplamSatis);
        const orders = rows.map(r => r.siparisSayisi);

        if (trendChart) trendChart.destroy(); // filtre değişince grafiği yeniden çiz

        trendChart = new Chart(document.getElementById('chartTrend'), {
            data: {
                labels,
                datasets: [
                    {
                        type: 'bar', label: 'Satış tutarı (₺)', data: sales, yAxisID: 'ySales',
                        backgroundColor: 'rgba(126, 166, 240, 0.55)', borderRadius: 6
                    },
                    {
                        type: 'line', label: 'Sipariş sayısı', data: orders, yAxisID: 'yOrders',
                        borderColor: '#f4b183', backgroundColor: '#f4b183', tension: 0.3, pointRadius: 3
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: ctx => ctx.dataset.yAxisID === 'ySales'
                                ? ` ${ctx.dataset.label}: ${moneyWhole.format(ctx.parsed.y)}`
                                : ` ${ctx.dataset.label}: ${numFmt.format(ctx.parsed.y)}`
                        }
                    }
                },
                scales: {
                    ySales: {
                        position: 'left', beginAtZero: true,
                        ticks: { callback: v => (v / 1e6).toLocaleString('tr-TR') + ' Mn ₺' }
                    },
                    yOrders: {
                        position: 'right', beginAtZero: true, grid: { drawOnChartArea: false },
                        ticks: { callback: v => numFmt.format(v) }
                    }
                }
            }
        });

        // İlk veya son ay tam değilse kullanıcıyı uyar (kısmi ay düşük görünür)
        const startDay = Number(startIso.slice(8, 10));
        const lastIncl = minusOneDay(endExclusiveIso);
        const [ly, lm, ld] = lastIncl.split('-').map(Number);
        const daysInLastMonth = new Date(ly, lm, 0).getDate();
        const partial = [];
        if (startDay > 1) partial.push('ilk');
        if (ld < daysInLastMonth) partial.push('son');
        document.getElementById('trendNote').textContent = partial.length
            ? `Not: ${partial.join(' ve ')} ay tam değil (seçili tarih aralığı ayı kısmen kapsıyor), bu yüzden o ay düşük görünebilir.`
            : '';
    }

    // Yatay sütun grafiği: ilk 10 sıralamaları için ortak çizici
    function renderRanking(key, canvasId, labels, values, extras, color) {
        if (rankCharts[key]) rankCharts[key].destroy();

        rankCharts[key] = new Chart(document.getElementById(canvasId), {
            type: 'bar',
            data: {
                labels,
                datasets: [{ label: 'Ciro (₺)', data: values, backgroundColor: color, borderRadius: 6 }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => ` Ciro: ${moneyWhole.format(ctx.parsed.x)}`,
                            afterLabel: ctx => extras[ctx.dataIndex]
                        }
                    }
                },
                scales: {
                    x: { beginAtZero: true, ticks: { callback: v => (v / 1e6).toLocaleString('tr-TR') + ' Mn ₺' } },
                    y: { grid: { display: false } }
                }
            }
        });
    }

    // Sipariş durumu dağılımı: halka grafik (iptal/iade dahil, bilerek)
    function renderStatus(rows) {
        if (statusChart) statusChart.destroy();

        statusChart = new Chart(document.getElementById('chartStatus'), {
            type: 'doughnut',
            data: {
                labels: rows.map(r => r.durum),
                datasets: [{
                    data: rows.map(r => r.siparisSayisi),
                    backgroundColor: rows.map(r => statusColors[r.durum] || '#cbd5e1'),
                    borderColor: '#fff', borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '58%',
                plugins: {
                    legend: { position: 'right' },
                    tooltip: {
                        callbacks: {
                            label: ctx => ` ${ctx.label}: ${numFmt.format(ctx.parsed)} sipariş (%${rows[ctx.dataIndex].yuzde.toLocaleString('tr-TR')})`
                        }
                    }
                }
            }
        });
    }

    // En çok satan ürünler: seçime göre ciro ya da adet gösterilir
    function renderProducts(rows, sortBy) {
        if (productsChart) productsChart.destroy();

        const byQty = sortBy === 'Adet';
        productsChart = new Chart(document.getElementById('chartProducts'), {
            type: 'bar',
            data: {
                labels: rows.map(r => r.productName),
                datasets: [{
                    label: byQty ? 'Satış adedi' : 'Ciro (₺)',
                    data: rows.map(r => byQty ? r.satisAdedi : r.toplamCiro),
                    backgroundColor: 'rgba(244, 177, 131, 0.7)', borderRadius: 6
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: ctx => byQty
                                ? ` Satış adedi: ${numFmt.format(ctx.parsed.x)}`
                                : ` Ciro: ${moneyWhole.format(ctx.parsed.x)}`,
                            afterLabel: ctx => {
                                const r = rows[ctx.dataIndex];
                                return `${r.categoryName} · ` + (byQty
                                    ? `Ciro: ${moneyWhole.format(r.toplamCiro)}`
                                    : `Satış adedi: ${numFmt.format(r.satisAdedi)}`);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        ticks: { callback: v => byQty ? numFmt.format(v) : (v / 1e6).toLocaleString('tr-TR') + ' Mn ₺' }
                    },
                    y: { grid: { display: false } }
                }
            }
        });
    }

    async function load() {
        if (dStart.value && dEnd.value && dStart.value > dEnd.value) {
            status.textContent = 'Başlangıç tarihi bitiş tarihinden sonra olamaz.';
            return;
        }

        const myId = ++requestId;
        status.textContent = 'Yükleniyor...';

        const p = new URLSearchParams();
        if (dStart.value) p.set('startDate', dStart.value);
        if (dEnd.value) p.set('endDate', dEnd.value);
        p.set('productSort', productSort.value);

        try {
            const res = await fetch('/Dashboard/Data?' + p.toString());
            const data = await res.json();
            if (myId !== requestId) return;

            if (!res.ok) {
                status.textContent = data.error || 'Bir hata oluştu.';
                return;
            }

            // Sunucunun uyguladığı (varsayılan dahil) aralığı forma yansıt
            dStart.value = dateOnly(data.startDate);
            dEnd.value = minusOneDay(data.endDate);
            status.textContent = '';

            renderKpis(data.kpis);
            renderTrend(data.monthlyTrend, data.startDate, data.endDate);

            renderRanking('cities', 'chartCities',
                data.topCities.map(c => c.cityName),
                data.topCities.map(c => c.toplamCiro),
                data.topCities.map(c => `Sipariş: ${numFmt.format(c.siparisSayisi)}`),
                'rgba(127, 209, 176, 0.7)');

            renderRanking('categories', 'chartCategories',
                data.topCategories.map(c => c.categoryName),
                data.topCategories.map(c => c.toplamCiro),
                data.topCategories.map(c => `Satış adedi: ${numFmt.format(c.satisAdedi)}`),
                'rgba(180, 167, 232, 0.7)');

            renderStatus(data.statusDistribution);
            renderProducts(data.topProducts, productSort.value);
        } catch (err) {
            if (myId !== requestId) return;
            status.textContent = 'Sunucuya ulaşılamadı.';
        }
    }

    form.addEventListener('submit', e => {
        e.preventDefault();
        load();
    });

    // Alanları boşaltınca sunucu varsayılan aralığı (son 12 ay) uygular
    dReset.addEventListener('click', () => {
        dStart.value = '';
        dEnd.value = '';
        load();
    });

    productSort.addEventListener('change', load);

    load();
})();