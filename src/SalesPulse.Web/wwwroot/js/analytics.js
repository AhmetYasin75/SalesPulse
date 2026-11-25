// Satış Analizi: tarih filtresi, dönem karşılaştırması, tekrar alışveriş oranı, tablolar ve kategori grafiği.
// Veri /Analytics/Data ucundan gelir, toplulaştırma SQL'de yapılır.
(function () {
    const form = document.getElementById('anFilter');
    const aStart = document.getElementById('aStart');
    const aEnd = document.getElementById('aEnd');
    const aReset = document.getElementById('aReset');
    const status = document.getElementById('anStatus');

    const numFmt = new Intl.NumberFormat('tr-TR');
    const moneyWhole = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 0 });
    const moneyCents = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });
    const pctFmt = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 1 });
    const dayFmt = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium' });

    const monthNames = ['Oca', 'Şub', 'Mar', 'Nis', 'May', 'Haz', 'Tem', 'Ağu', 'Eyl', 'Eki', 'Kas', 'Ara'];
    const lineColors = ['#7ea6f0', '#7fd1b0', '#b4a7e8', '#f4b183', '#f3a6b8']; // pastel paletin çizgi tonları

    let requestId = 0; // geç gelen eski yanıtların yenisini ezmesini engeller
    let catChart = null;

    // Metni güvenle HTML'e yazmak için kaçışlar
    function esc(value) {
        const div = document.createElement('div');
        div.textContent = value ?? '';
        return div.innerHTML;
    }

    // "2026-11-19T00:00:00" -> "2026-11-19"
    const dateOnly = iso => iso.slice(0, 10);

    // Sunucudan gelen bitiş tarihi "hariç" sınırdır; ekranda "dahil" son gün olarak gösterilir
    function minusOneDay(iso) {
        const [y, m, d] = dateOnly(iso).split('-').map(Number);
        const dt = new Date(y, m - 1, d - 1);
        const pad = n => String(n).padStart(2, '0');
        return `${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(dt.getDate())}`;
    }

    // "2024-11-19" -> yerel tarih nesnesi (saat dilimi kaymasın diye parçalanarak oluşturulur)
    function localDate(isoDay) {
        const [y, m, d] = isoDay.split('-').map(Number);
        return new Date(y, m - 1, d);
    }

    function setText(id, text) {
        document.getElementById(id).textContent = text;
    }

    // Bir karşılaştırma kartını doldurur: güncel değer, önceki dönem ve yüzde değişim
    function fillCompare(id, current, previous, fmt) {
        setText(id, fmt.format(current));

        const sub = document.getElementById(id + 'Sub');
        let change = '';
        if (previous > 0) {
            const pct = (current - previous) / previous * 100;
            const up = pct >= 0;
            change = ` <span class="${up ? 'chg-up' : 'chg-down'}">${up ? '▲' : '▼'} %${pctFmt.format(Math.abs(pct))}</span>`;
        }
        sub.innerHTML = `Önceki dönem: ${fmt.format(previous)}${change}`;
    }

    function renderComparison(rows, data) {
        const cur = rows.find(r => r.donem === 1);
        const prev = rows.find(r => r.donem === 2);
        if (!cur || !prev) return;

        fillCompare('cmpOrders', cur.siparisSayisi, prev.siparisSayisi, numFmt);
        fillCompare('cmpRevenue', cur.toplamCiro, prev.toplamCiro, moneyWhole);
        fillCompare('cmpCustomers', cur.aktifMusteri, prev.aktifMusteri, numFmt);
        fillCompare('cmpBasket', cur.ortalamaSepet, prev.ortalamaSepet, moneyCents);

        const f = iso => dayFmt.format(localDate(dateOnly(iso)));
        const l = iso => dayFmt.format(localDate(minusOneDay(iso)));
        setText('cmpNote',
            `Seçili dönem: ${f(data.startDate)} – ${l(data.endDate)} · ` +
            `Karşılaştırılan dönem: ${f(data.compareStartDate)} – ${l(data.compareEndDate)} (aynı uzunlukta önceki dönem)`);
    }

    function renderRepeat(r) {
        setText('repeatRate', `%${r.tekrarOrani.toLocaleString('tr-TR')}`);
        setText('repeatSub', `${numFmt.format(r.tekrarEdenMusteri)} / ${numFmt.format(r.aktifMusteri)} aktif müşteri en az 2 sipariş verdi`);
    }

    // İlk 20 müşteri: aynı ad-soyad farklı müşterilerde tekrar edebildiği için müşteri no da gösterilir
    function renderCustomers(rows) {
        document.getElementById('customersBody').innerHTML = rows.length
            ? rows.map((c, i) => `<tr>
                <td>${i + 1}</td>
                <td>${esc(c.musteriAdi)} <span class="muted">#${c.customerId}</span></td>
                <td>${esc(c.sehir)}</td>
                <td class="num">${numFmt.format(c.siparisSayisi)}</td>
                <td class="num">${moneyWhole.format(c.toplamCiro)}</td>
            </tr>`).join('')
            : '<tr><td colspan="5" class="empty">Bu aralıkta kayıt yok.</td></tr>';
    }

    // En çok satış yapılan şehirler
    function renderCities(rows) {
        document.getElementById('citiesBody').innerHTML = rows.length
            ? rows.map((c, i) => `<tr>
                <td>${i + 1}</td>
                <td>${esc(c.cityName)}</td>
                <td>${esc(c.region)}</td>
                <td class="num">${numFmt.format(c.siparisSayisi)}</td>
                <td class="num">${moneyWhole.format(c.toplamCiro)}</td>
            </tr>`).join('')
            : '<tr><td colspan="5" class="empty">Bu aralıkta kayıt yok.</td></tr>';
    }

    // En yüksek gelir sağlayan ürünler
    function renderProducts(rows) {
        document.getElementById('productsBody').innerHTML = rows.length
            ? rows.map((p, i) => `<tr>
                <td>${i + 1}</td>
                <td>${esc(p.productName)}</td>
                <td>${esc(p.categoryName)}</td>
                <td class="num">${numFmt.format(p.satisAdedi)}</td>
                <td class="num">${moneyWhole.format(p.toplamCiro)}</td>
            </tr>`).join('')
            : '<tr><td colspan="5" class="empty">Bu aralıkta kayıt yok.</td></tr>';
    }

    // Kategorilerin aylık cirosu: her kategori bir çizgi, her ay bir nokta
    function renderCategoryMonthly(rows) {
        if (catChart) catChart.destroy(); // filtre değişince grafiği yeniden çiz

        // Ay etiketleri: veride geçen (yıl, ay) çiftleri kronolojik sırayla
        const months = [...new Map(rows.map(r => [r.yil * 100 + r.ay, r])).values()]
            .sort((a, b) => (a.yil * 100 + a.ay) - (b.yil * 100 + b.ay));
        const labels = months.map(m => `${monthNames[m.ay - 1]} ${String(m.yil).slice(2)}`);
        const monthIndex = new Map(months.map((m, i) => [m.yil * 100 + m.ay, i]));

        // Kategoriler: toplam cirosu en yüksekten başlayarak (renkler sabit kalsın)
        const totals = new Map();
        rows.forEach(r => {
            const t = totals.get(r.categoryId) || { name: r.categoryName, sum: 0 };
            t.sum += r.toplamCiro;
            totals.set(r.categoryId, t);
        });
        const categories = [...totals.entries()].sort((a, b) => b[1].sum - a[1].sum);

        const datasets = categories.map(([id, info], i) => {
            const data = new Array(labels.length).fill(null); // o ayda satış yoksa boş bırak
            rows.filter(r => r.categoryId === id)
                .forEach(r => { data[monthIndex.get(r.yil * 100 + r.ay)] = r.toplamCiro; });
            return {
                label: info.name, data,
                borderColor: lineColors[i % lineColors.length],
                backgroundColor: lineColors[i % lineColors.length],
                borderWidth: 2, tension: 0.3, pointRadius: 3
            };
        });

        catChart = new Chart(document.getElementById('chartCatMonthly'), {
            type: 'line',
            data: { labels, datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    tooltip: {
                        callbacks: { label: ctx => ` ${ctx.dataset.label}: ${moneyWhole.format(ctx.parsed.y)}` }
                    }
                },
                scales: {
                    y: { beginAtZero: true, ticks: { callback: v => (v / 1e6).toLocaleString('tr-TR') + ' Mn ₺' } }
                }
            }
        });
    }

    async function load() {
        if (aStart.value && aEnd.value && aStart.value > aEnd.value) {
            status.textContent = 'Başlangıç tarihi bitiş tarihinden sonra olamaz.';
            return;
        }

        const myId = ++requestId;
        status.textContent = 'Yükleniyor...';

        const p = new URLSearchParams();
        if (aStart.value) p.set('startDate', aStart.value);
        if (aEnd.value) p.set('endDate', aEnd.value);

        try {
            const res = await fetch('/Analytics/Data?' + p.toString());
            const data = await res.json();
            if (myId !== requestId) return;

            if (!res.ok) {
                status.textContent = data.error || 'Bir hata oluştu.';
                return;
            }

            // Sunucunun uyguladığı (varsayılan dahil) aralığı forma yansıt
            aStart.value = dateOnly(data.startDate);
            aEnd.value = minusOneDay(data.endDate);
            status.textContent = '';

            renderComparison(data.comparison, data);
            renderRepeat(data.repeatRate);
            renderCustomers(data.topCustomers);
            renderCities(data.topCities);
            renderCategoryMonthly(data.categoryMonthly);
            renderProducts(data.topProducts);
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
    aReset.addEventListener('click', () => {
        aStart.value = '';
        aEnd.value = '';
        load();
    });

    load();
})();