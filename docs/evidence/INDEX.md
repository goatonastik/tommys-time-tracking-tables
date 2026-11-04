# Preserved release evidence

Copied 2026-11-04 from ignored local artifacts. These are historical 2026-11-03 records for 0.5.3, not newly executed checks. Raw reviewer input snapshots, personal workspaces and credentials are excluded. Paths and process IDs in copied logs describe the original test environment.

| File | Meaning and limit |
| --- | --- |
| claude-0.5.3-final.md | Final static read-only review; no confirmed defect, optional test gaps remain; reviewer executed nothing |
| build-0.5.3.log | 67 passed, zero failed, zero build warnings/errors |
| app-dependency-audit-0.5.3.log / dependency-audit-0.5.3.log | Historical direct/transitive advisory queries; no listed vulnerable packages then, not a future guarantee |
| defender-0.5.3.json | Exact installer scan with zero matching detections; no certification |
| launch-smoke-0.5.3.json | Packaged executable opened/responded with isolated data; process terminated afterward, clean close unverified |

Release: https://github.com/goatonastik/tommys-time-tracking-tables/releases/tag/v0.5.3-preview
Source: 8125bbf434c0767069432f2e05fda153e7329bc1 (remote), 617b5af (local implementation).
GitHub workflow 41788143140, verify job 126434952972 passed according to release records.

| Published file | Bytes | SHA-256 |
| --- | ---: | --- |
| Tommys-Time-Tracking-Tables-Setup-0.5.3.exe | 49,412,916 | 58CA731D576CE6A7445A915B1238326605829F6F622FF29C2E50163EB66E17AA |
| Tommys-Time-Tracking-Tables-win-x64.zip | 69,596,853 | C0A5CE90E6EC9008DF3EF53C3E4B72F8092348CA608909B156497F49457EFAB2 |

Both downloads are unsigned. Checksums identify content; they do not establish publisher identity. Original local artifact references and fuller chronology remain in CHECKPOINT.md and PROGRESS.md.

## Evidence file integrity

Hashes identify LF-normalized preserved copies; they are not a certification:

- app-dependency-audit-0.5.3.log: 568CDC0CCD87AD423AD7A4728F77CFBF10834A130826CF40AC7FAA8944EEC5D6
- build-0.5.3.log: A3F0EAA0ED70182C1F68F16ABF793462F3746EE95CABC9C43A0243597776A3B7
- claude-0.5.3-final.md: 34C05542F9EB20210E642979579B873E4184634F567F0656A8C3761019995826
- defender-0.5.3.json: 92A28A00A763A8F1F523F347F56122395482F8FB1E68FEC9AAB7B3901A9A2D73
- dependency-audit-0.5.3.log: 39FFD5E6C4ED364E02D933B7588F63CD23DEE95DAE97ED61159430F3BEB85799
- launch-smoke-0.5.3.json: F158B00EB9D90CB7DEA1490F12B7B2765C5C48CDC0302842EBCF462C3676B1DA
