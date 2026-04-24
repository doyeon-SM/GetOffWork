using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 런타임 방문객 UserRecordData 생성 전담 유틸리티.
///
/// [생성 규칙]
/// - ID        : 랜덤 8자리 숫자. DB 중복 시 정상 반려 사항(isDuplicateId=true).
/// - 이름       : 한글 2~4글자 (2글자 20%, 3글자 70%, 4글자 10%)
/// - 초상화     : PortraitListSO.portraits에서 랜덤 1개 → UserImage 표시 및 DB 기준
/// - 가짜초상화 : PortraitListSO.fakePortraits에서 랜덤 1개 → 신분증에 표시 (반려 사항)
/// - 주소       : AddressListSO(정상) / FakeAddressListSO(가짜) 에서 랜덤
/// - 생일       : 1950~2010 사이 랜덤 날짜
/// - 전화번호   : 010-XXXX-XXXX
/// - 이메일     : {이름}@mail.com
/// - 가짜ID     : 8자리 랜덤 숫자 (실제 ID와 다르게)
/// </summary>
public static class RuntimeUserGenerator
{
    private const string TAG = "[RuntimeUserGenerator]";

    // ── 한글 구성 데이터 ──────────────────────────────────────────────────
    private static readonly string[] FirstNames =
    {
        "김","이","박","최","정","강","조","윤","장","임","한","오","서","신","권",
        "황","안","송","류","전","홍","고","문","양","손","배","백","허","유","남",
        "심","노","하","곽","성","차","주","우","구","민","나","진","지","엄","채",
        "원","천","방","공","현","함","변","염","여","추","도","소","석","선","설"
    };

    private static readonly string[] MiddleNames =
    {
        "민","서","지","예","수","채","유","나","다","하","주","아","소","은","재",
        "현","세","태","진","승","영","준","성","동","혁","원","호","기","연","정"
    };

    private static readonly string[] LastNames =
    {
        "준","우","린","아","은","현","진","서","연","수","영","희","리","나","혜",
        "인","원","율","빈","솔","하","야","온","담","결","빛","봄","찬","휘","도"
    };

    // ── 주 생성 메서드 ────────────────────────────────────────────────────

    /// <summary>
    /// 방문객 UserRecordData를 런타임에 생성한다.
    /// ScriptableObject.CreateInstance를 사용하므로 게임 종료 시 자동 소멸.
    /// </summary>
    /// <param name="database">중복 ID 검사에 사용할 DB</param>
    /// <param name="portraitListSO">초상화 리스트 SO</param>
    /// <param name="addressListSO">주소 리스트 SO (정상 주소)</param>
    /// <param name="fakeAddressListSO">가짜 주소 리스트 SO</param>
    /// <param name="isDuplicateId">생성된 ID가 DB에 이미 존재하면 true</param>
public static UserRecordData Generate(
        UserRecordDatabase database,
        PortraitListSO     portraitListSO,
        AddressListSO      addressListSO,
        AddressListSO      fakeAddressListSO,
        out bool           isDuplicateId)
    {
        isDuplicateId = false;

        var data = ScriptableObject.CreateInstance<UserRecordData>();

        // ── ID ────────────────────────────────────────────────────────────
        string id = GenerateId();
        if (database != null && database.TryGetRecord(id, out _))
        {
            isDuplicateId = true;
            Debug.LogWarning(TAG + $" ID 중복 감지: {id} — 반려 사항으로 처리됩니다.");
        }
        data.recordId = id;

        // ── 이름 ────────────────────────────────────────────────────────────
        data.fullName = GenerateKoreanName();

        // ── 초상화 (정상 + 가짜) ────────────────────────────────────────────
        data.portrait     = portraitListSO?.GetRandomPortrait();
        data.fakePortrait = portraitListSO?.GetRandomFakePortrait();

        // ── 주소 (정상 + 가짜) ───────────────────────────────────────────
        data.address     = GetRandomFromList(addressListSO?.addresses, "(주소 없음)");
        data.fakeAddress = GetRandomFromList(fakeAddressListSO?.addresses, string.Empty);

        // ── 생일 ────────────────────────────────────────────────────────────
        data.birthDate = GenerateBirthDate();

        // ── 전화번호 ─────────────────────────────────────────────────────────
        data.phoneNumber = GeneratePhoneNumber();

        // ── 이메일 ─────────────────────────────────────────────────────────
        data.email = $"{data.fullName}@mail.com";

        // ── 가짜 ID ────────────────────────────────────────────────────────
        data.fakeID = GenerateFakeId(data.recordId);


        // IdCard* 필드 제거됨: 표시값은 Spawn 시점(ObjectManagerBox)에 직접 계산한다.
        Debug.Log(TAG + $" 방문객 생성 완료 — ID:{data.recordId} / 이름:{data.fullName}");
        return data;
    }

    // ── ID 생성 ───────────────────────────────────────────────────────────

    private static string GenerateId()
    {
        return UnityEngine.Random.Range(10000000, 100000000).ToString();
    }

    /// <summary>실제 ID와 다른 8자리 가짜 ID를 생성한다.</summary>
    private static string GenerateFakeId(string realId)
    {
        string fakeId;
        int attempts = 0;
        do
        {
            fakeId = UnityEngine.Random.Range(10000000, 100000000).ToString();
            attempts++;
        }
        while (fakeId == realId && attempts < 100);
        return fakeId;
    }

    // ── 한글 이름 생성 ────────────────────────────────────────────────────

    private static string GenerateKoreanName()
    {
        // 2글자 20%, 3글자 70%, 4글자 10%
        float roll = UnityEngine.Random.value;
        int length = roll < 0.20f ? 2 : roll < 0.90f ? 3 : 4;

        string firstName = FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)];

        if (length == 2)
        {
            string last = LastNames[UnityEngine.Random.Range(0, LastNames.Length)];
            return firstName + last;
        }
        else if (length == 3)
        {
            string mid  = MiddleNames[UnityEngine.Random.Range(0, MiddleNames.Length)];
            string last = LastNames[UnityEngine.Random.Range(0, LastNames.Length)];
            return firstName + mid + last;
        }
        else // 4글자
        {
            string mid1 = MiddleNames[UnityEngine.Random.Range(0, MiddleNames.Length)];
            string mid2 = MiddleNames[UnityEngine.Random.Range(0, MiddleNames.Length)];
            string last = LastNames[UnityEngine.Random.Range(0, LastNames.Length)];
            return firstName + mid1 + mid2 + last;
        }
    }

    // ── 생일 생성 ─────────────────────────────────────────────────────────

    private static string GenerateBirthDate()
    {
        int year  = UnityEngine.Random.Range(1950, 2011);
        int month = UnityEngine.Random.Range(1, 13);
        int maxDay = System.DateTime.DaysInMonth(year, month);
        int day   = UnityEngine.Random.Range(1, maxDay + 1);
        return $"{year}.{month:D2}.{day:D2}";
    }

    // ── 전화번호 생성 ─────────────────────────────────────────────────────

    private static string GeneratePhoneNumber()
    {
        int mid  = UnityEngine.Random.Range(1000, 10000);
        int last = UnityEngine.Random.Range(1000, 10000);
        return $"010-{mid:D4}-{last:D4}";
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────

    private static string GetRandomFromList(System.Collections.Generic.List<string> list, string fallback)
    {
        if (list == null || list.Count == 0) return fallback;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }
}
