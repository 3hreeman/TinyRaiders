from pathlib import Path
import json, shutil, hashlib

build = Path('Builds/ArcadeWebGL')
target = Path('Temp/ArcadeIntegration/apps/web/public/unity/survival-legend')
assert Path('Logs/ArcadeWebBuild.txt').read_text().startswith('Succeeded')
for old_part in (target/'Build').glob('*.unityweb.*.part*'):
    assert old_part.resolve().is_relative_to(target.resolve())
    old_part.unlink()
for part in ['Build', 'StreamingAssets']:
    if (build/part).exists():
        shutil.copytree(build/part, target/part, dirs_exist_ok=True)
def asset(suffix):
    files = list((target/'Build').glob('*'+suffix))
    assert len(files) == 1, (suffix, files)
    return 'Build/' + files[0].name
config = dict(dataUrl=asset('.data.unityweb'), frameworkUrl=asset('.framework.js.unityweb'),
              codeUrl=asset('.wasm.unityweb'), streamingAssetsUrl='StreamingAssets',
              companyName='TinyRaiders', productName='Survival Legend', productVersion='1.0')
chunks = {}
for key in ['dataUrl', 'codeUrl']:
    path = target/config[key]
    if path.stat().st_size <= 20*1024*1024:
        continue
    payload = path.read_bytes()
    digest = hashlib.sha256(payload).hexdigest()[:16]
    chunks[key] = []
    for i, start in enumerate(range(0,len(payload),20*1024*1024)):
        part = payload[start:start+20*1024*1024]
        relative = 'Build/' + path.name + '.' + digest + '.part' + str(i)
        (target/relative).write_bytes(part)
        chunks[key].append({'url':relative,'bytes':len(part)})
    # Only remove the just-created staging copy; keep the original Unity build.
    assert path.resolve().is_relative_to(target.resolve())
    path.unlink()
html = '''<!doctype html>
<html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="tiny-raiders-player" content="1">
<title>Tiny Raiders · Survival Legend</title>
<style>
html,body{margin:0;width:100%;height:100%;overflow:hidden;background:#05090d;color:#e4dbc3;font-family:system-ui,sans-serif}
canvas{display:block;width:100%;height:100%;outline:0}
#loading{position:fixed;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;background:#0b1418;gap:16px}
progress{width:min(360px,80vw);accent-color:#c6a969}#error{max-width:80vw;color:#ffb5af;white-space:pre-wrap}
</style></head><body><canvas id="game" tabindex="0" aria-label="Survival Legend"></canvas>
<div id="loading"><h1>Survival Legend</h1><div id="status" role="status">전장을 준비하고 있습니다…</div><progress id="progress" max="1" value="0"></progress><div id="error" role="alert"></div></div>
<script>
const canvas=document.getElementById('game');
const notify=(type)=>{if(window.parent!==window)window.parent.postMessage({type},window.location.origin);};
const fail=(message)=>{document.getElementById('loading').style.display='flex';document.getElementById('status').textContent='게임을 불러오지 못했습니다.';document.getElementById('error').textContent=String(message);notify('tiny-raiders-error');};
canvas.addEventListener('contextmenu',event=>event.preventDefault());
canvas.addEventListener('wheel',event=>event.preventDefault(),{passive:false});
canvas.addEventListener('pointerdown',()=>canvas.focus());
window.addEventListener('focus',()=>canvas.focus());
const config=__CONFIG__;
const chunked=__CHUNKS__;
const objectUrls=[];
config.cacheControl=()=> 'no-store';
config.showBanner=(message,type)=>{if(type==='error')fail(message);else console.warn(message);};
async function loadChunks(){
  const total=Object.values(chunked).flat().reduce((n,part)=>n+part.bytes,0);let loaded=0;
  for(const [key,parts] of Object.entries(chunked)){
    const buffers=[];
    for(const part of parts){
      const response=await fetch(part.url);if(!response.ok)throw new Error('리소스 다운로드 실패: '+response.status);
      const buffer=await response.arrayBuffer();if(buffer.byteLength!==part.bytes)throw new Error('리소스 파일이 불완전합니다. 다시 시도해주세요.');
      buffers.push(buffer);loaded+=buffer.byteLength;document.getElementById('progress').value=.6*loaded/total;
    }
    const url=URL.createObjectURL(new Blob(buffers,{type:'application/octet-stream'}));objectUrls.push(url);config[key]=url;
  }
}
const script=document.createElement('script');script.src=__LOADER__;
script.onerror=()=>fail('게임 파일을 찾을 수 없습니다. 페이지를 새로고침해주세요.');
script.onload=()=>loadChunks().then(()=>createUnityInstance(canvas,config,value=>document.getElementById('progress').value=Object.keys(chunked).length ? .6+.4*value : value)).then(instance=>{
  window.unityInstance=instance;document.getElementById('loading').style.display='none';canvas.focus();notify('tiny-raiders-ready');
}).catch(fail).finally(()=>objectUrls.forEach(url=>URL.revokeObjectURL(url)));
document.body.appendChild(script);
</script></body></html>
'''.replace('__CONFIG__', json.dumps(config)).replace('__CHUNKS__',json.dumps(chunks)).replace('__LOADER__', json.dumps(asset('.loader.js')))
(target/'index.html').write_text(html,encoding='utf-8')
files = [{ 'path': str(p.relative_to(target)).replace('\\','/'), 'bytes':p.stat().st_size,
           'sha256':hashlib.sha256(p.read_bytes()).hexdigest() } for p in target.rglob('*') if p.is_file() and p.name != 'build-manifest.json']
(target/'build-manifest.json').write_text(json.dumps({'unity':'6000.5.4f1','scene':'SurvivalLegendGameObjects','files':files},indent=2),encoding='utf-8')
for f in files:
    print(f['path'],f['bytes'])
    assert f['bytes'] < 25*1024*1024, 'Asset exceeds 25 MiB static hosting limit'
print('Packaged',sum(f['bytes'] for f in files),'bytes')
