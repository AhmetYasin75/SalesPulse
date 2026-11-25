// Order Explorer: liste, filtre, sıralama, sayfalama ve detay penceresi.
// Veri /Orders/Data ucundan gelir, tüm işlem SQL'de yapılır.
(function () {
    const state = { pageNumber: 1, pageSize: 20, totalPages: 1, sortBy: 'Tarih', sortDir: 'DESC' };
    let requestId = 0; // geç gelen eski yanıtların yenisini ezmesini engeller

    const body = document.getElementById('ordersBody');
    const info = document.getElementById('ordersInfo');
    const sizeSelect = document.getElementById('pageSize');
    const btnFirst = document.getElementById('btnFirst');
    const btnPrev = document.getElementById('btnPrev');
    const btnNext = document.getElementById('btnNext');
    const btnLast = document.getElementById('btnLast');

    const form = document.getElementById('filterForm');
    const fSearch = document.getElementById('fSearch');
    const fStatus = document.getElementById('fStatus');
    const fCity = document.getElementById('fCity');
    const fStart = document.getElementById('fStart');
    const fEnd = document.getElementById('fEnd');
    const fMin = document.getElementById('fMin');
    const fMax = document.getElementById('fMax');
    const fReset = document.getElementById('fReset');

    const money = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' });
    const dateFmt = new Intl.DateTimeFormat('tr-TR', { dateStyle: 'short', timeStyle: 'short' });
    const numFmt = new Intl.NumberFormat('tr-TR');

    const statusClass = {
        'Tamamlandı': 'mint',
        'Beklemede': 'orange',
        'Kargoda': 'blue',
        'İptal Edildi': 'lavender',
        'İade Edildi': 'lavender'
    };

    // Metni güvenle HTML'e yazmak için kaçışlar
    function esc(value) {
        const div = document.createElement('div');
        div.textContent = value ?? '';
        return div.innerHTML;
    }

    function message(text) {
        body.innerHTML = `<tr><td colspan="7" class="empty">${esc(text)}</td></tr>`;
    }

    // "2026-02-10" -> "2026-02-11" (bitiş tarihi dahil olsun diye SQL'e ertesi günü göndeririz)
    function addOneDay(isoDate) {
        const [y, m, d] = isoDate.split('-').map(Number);
        const dt = new Date(y, m - 1, d + 1);
        const pad = n => String(n).padStart(2, '0');
        return `${dt.getFullYear()}-${pad(dt.getMonth() + 1)}-${pad(dt.getDate())}`;
    }

    function rowHtml(o) {
        const cls = statusClass[o.durum] || 'blue';
        return `<tr class="clickable" data-id="${o.siparisNo}">
            <td>${o.siparisNo}</td>
            <td>${esc(o.musteri)}</td>
            <td>${esc(o.sehir)}</td>
            <td>${dateFmt.format(new Date(o.tarih))}</td>
            <td class="num">${money.format(o.tutar)}</td>
            <td>${esc(o.odemeYontemi)}</td>
            <td><span class="badge ${cls}">${esc(o.durum)}</span></td>
        </tr>`;
    }

    function updatePager() {
        const atFirst = state.pageNumber <= 1;
        const atLast = state.pageNumber >= state.totalPages;
        btnFirst.disabled = atFirst;
        btnPrev.disabled = atFirst;
        btnNext.disabled = atLast;
        btnLast.disabled = atLast;
    }

    // Formdaki dolu filtreleri ve sıralamayı sorgu parametrelerine çevirir
    function buildParams() {
        const p = new URLSearchParams({ pageNumber: state.pageNumber, pageSize: state.pageSize });

        const search = fSearch.value.trim();
        if (search) p.set('search', search);
        if (fStatus.value) p.set('status', fStatus.value);
        if (fCity.value) p.set('cityId', fCity.value);
        if (fStart.value) p.set('startDate', fStart.value);
        if (fEnd.value) p.set('endDate', addOneDay(fEnd.value));
        if (fMin.value !== '') p.set('minAmount', fMin.value);
        if (fMax.value !== '') p.set('maxAmount', fMax.value);
        p.set('sortBy', state.sortBy);
        p.set('sortDir', state.sortDir);
        return p;
    }

    async function load() {
        // Tarayıcı tarafında hızlı doğrulama (sunucu da ayrıca doğrular)
        if (fStart.value && fEnd.value && fStart.value > fEnd.value) {
            message('Başlangıç tarihi bitiş tarihinden sonra olamaz.');
            info.textContent = '';
            return;
        }

        const myId = ++requestId;
        message('Yükleniyor...');

        try {
            const res = await fetch('/Orders/Data?' + buildParams().toString());
            const data = await res.json();
            if (myId !== requestId) return; // daha yeni bir istek var

            if (!res.ok) {
                message(data.error || 'Bir hata oluştu.');
                return;
            }

            state.totalPages = Math.max(data.totalPages, 1);

            if (data.items.length) {
                body.innerHTML = data.items.map(rowHtml).join('');
                info.textContent = `Toplam ${numFmt.format(data.totalCount)} kayıt · Sayfa ${numFmt.format(data.pageNumber)} / ${numFmt.format(state.totalPages)}`;
            } else {
                message('Bu filtrelere uyan kayıt bulunamadı.');
                info.textContent = 'Toplam 0 kayıt';
            }
            updatePager();
        } catch (err) {
            if (myId !== requestId) return;
            message('Sunucuya ulaşılamadı.');
        }
    }

    function goTo(page) {
        state.pageNumber = Math.min(Math.max(page, 1), state.totalPages);
        load();
    }

    // Şehir listesini sunucudan doldurur (81 şehir, adına göre sıralı)
    async function loadCities() {
        try {
            const res = await fetch('/Orders/Cities');
            if (!res.ok) return;
            const cities = await res.json();
            fCity.insertAdjacentHTML('beforeend',
                cities.map(c => `<option value="${c.cityId}">${esc(c.cityName)}</option>`).join(''));
        } catch (err) {
            // Liste yüklenemezse sayfa şehir filtresi olmadan çalışmaya devam eder
        }
    }

    btnFirst.addEventListener('click', () => goTo(1));
    btnPrev.addEventListener('click', () => goTo(state.pageNumber - 1));
    btnNext.addEventListener('click', () => goTo(state.pageNumber + 1));
    btnLast.addEventListener('click', () => goTo(state.totalPages));

    sizeSelect.addEventListener('change', () => {
        state.pageSize = parseInt(sizeSelect.value, 10);
        state.pageNumber = 1;
        load();
    });

    // Filtrele (düğme ya da Enter): filtre değişince sayfa 1'e dön
    form.addEventListener('submit', e => {
        e.preventDefault();
        state.pageNumber = 1;
        load();
    });

    fReset.addEventListener('click', () => {
        form.reset();
        state.pageNumber = 1;
        load();
    });


    // CSV: o an seçili filtrelerle sunucudan en fazla 50.000 satır indirir
    document.getElementById('fExport').addEventListener('click', () => {
        if (fStart.value && fEnd.value && fStart.value > fEnd.value) {
            message('Başlangıç tarihi bitiş tarihinden sonra olamaz.');
            return;
        }
        window.location.href = '/Orders/ExportCsv?' + buildParams().toString();
    });

    // Başlığa tıklayınca sırala: aynı sütuna tekrar tıklamak yönü değiştirir
    const sortHeaders = document.querySelectorAll('th.sortable');

    function updateSortIndicators() {
        sortHeaders.forEach(th => {
            const active = th.dataset.sort === state.sortBy;
            th.querySelector('.sort-ind').textContent = active ? (state.sortDir === 'ASC' ? '▲' : '▼') : '';
        });
    }

    sortHeaders.forEach(th => th.addEventListener('click', () => {
        const col = th.dataset.sort;
        if (state.sortBy === col) {
            state.sortDir = state.sortDir === 'ASC' ? 'DESC' : 'ASC';
        } else {
            state.sortBy = col;
            state.sortDir = 'ASC';
        }
        state.pageNumber = 1;
        updateSortIndicators();
        load();
    }));

    // ---- Sipariş detay penceresi ----
    const modal = document.getElementById('detailModal');
    const detailTitle = document.getElementById('detailTitle');
    const detailBody = document.getElementById('detailBody');
    const detailClose = document.getElementById('detailClose');
    let detailRequestId = 0;

    function closeDetail() {
        detailRequestId++; // yoldaki eski yanıtı yok say
        modal.hidden = true;
    }

    function detailHtml(d) {
        const cls = statusClass[d.durum] || 'blue';
        const rows = d.kalemler.map(k => `<tr>
            <td>${k.kalemNo}</td>
            <td>${esc(k.urun)}</td>
            <td>${esc(k.kategori)}</td>
            <td class="num">${numFmt.format(k.adet)}</td>
            <td class="num">${money.format(k.birimFiyat)}</td>
            <td class="num">${money.format(k.satirToplami)}</td>
        </tr>`).join('');

        return `<dl class="detail-grid">
                <div><dt>Müşteri</dt><dd>${esc(d.musteri)}</dd></div>
                <div><dt>E-posta</dt><dd>${esc(d.eposta)}</dd></div>
                <div><dt>Şehir</dt><dd>${esc(d.sehir)}</dd></div>
                <div><dt>Tarih</dt><dd>${dateFmt.format(new Date(d.tarih))}</dd></div>
                <div><dt>Ödeme yöntemi</dt><dd>${esc(d.odemeYontemi)}</dd></div>
                <div><dt>Durum</dt><dd><span class="badge ${cls}">${esc(d.durum)}</span></dd></div>
            </dl>
            <div class="table-wrap">
                <table class="table">
                    <thead><tr>
                        <th>#</th><th>Ürün</th><th>Kategori</th>
                        <th class="num">Adet</th><th class="num">Birim fiyat</th><th class="num">Satır toplamı</th>
                    </tr></thead>
                    <tbody>${rows || '<tr><td colspan="6" class="empty">Kalem bulunamadı.</td></tr>'}</tbody>
                </table>
            </div>
            <div class="detail-total">Toplam: ${money.format(d.tutar)}</div>`;
    }

    async function openDetail(id) {
        const myId = ++detailRequestId;
        detailTitle.textContent = `Sipariş #${id}`;
        detailBody.innerHTML = '<p class="empty">Yükleniyor...</p>';
        modal.hidden = false;

        try {
            const res = await fetch(`/Orders/Detail/${id}`);
            const data = await res.json();
            if (myId !== detailRequestId) return;
            detailBody.innerHTML = res.ok
                ? detailHtml(data)
                : `<p class="empty">${esc(data.error || 'Detay alınamadı.')}</p>`;
        } catch (err) {
            if (myId !== detailRequestId) return;
            detailBody.innerHTML = '<p class="empty">Sunucuya ulaşılamadı.</p>';
        }
    }

    // Satır tıklaması: tbody'ye tek dinleyici (satırlar sürekli yeniden çiziliyor)
    body.addEventListener('click', e => {
        const tr = e.target.closest('tr.clickable');
        if (tr) openDetail(tr.dataset.id);
    });
    detailClose.addEventListener('click', closeDetail);
    modal.addEventListener('click', e => { if (e.target === modal) closeDetail(); });
    document.addEventListener('keydown', e => { if (e.key === 'Escape' && !modal.hidden) closeDetail(); });

    updateSortIndicators();
    loadCities();
    load();
})();